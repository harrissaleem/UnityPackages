// SimCore - Task Queue Module
// Generic task/job queue with worker pool for resource-constrained processing
// Use cases: Backup vehicles, delivery trucks, waiters, ambulances, etc.

using System;
using System.Collections.Generic;
using UnityEngine;
using SimCore.Signals;

namespace SimCore.Modules.TaskQueue
{
    /// <summary>
    /// Task submitted signal
    /// </summary>
    public struct TaskSubmittedSignal : ISignal
    {
        public string TaskId;
        public string QueueId;
        public string TaskType;
        public SimId TargetEntityId;
        public Vector3 Position;
    }
    
    /// <summary>
    /// Worker assigned to task
    /// </summary>
    public struct WorkerAssignedSignal : ISignal
    {
        public string TaskId;
        public string WorkerId;
        public string QueueId;
        public float EstimatedArrivalSeconds;
    }
    
    /// <summary>
    /// Worker arrived at task location
    /// </summary>
    public struct WorkerArrivedSignal : ISignal
    {
        public string TaskId;
        public string WorkerId;
        public string QueueId;
        public SimId TargetEntityId;
    }
    
    /// <summary>
    /// Task completed
    /// </summary>
    public struct TaskCompletedSignal : ISignal
    {
        public string TaskId;
        public string WorkerId;
        public string QueueId;
        public string TaskType;
        public SimId TargetEntityId;
        public Dictionary<string, int> Rewards;
    }
    
    /// <summary>
    /// Task cancelled/failed
    /// </summary>
    public struct TaskCancelledSignal : ISignal
    {
        public string TaskId;
        public string QueueId;
        public string Reason;
    }
    
    /// <summary>
    /// Task status
    /// </summary>
    public enum TaskStatus
    {
        Pending,        // Waiting for worker
        Assigned,       // Worker on the way
        InProgress,     // Worker arrived, processing
        Completed,
        Cancelled
    }
    
    /// <summary>
    /// Worker status
    /// </summary>
    public enum WorkerStatus
    {
        Available,      // Ready for assignment
        EnRoute,        // Traveling to task
        Working,        // Processing task
        Returning       // Going back to base/pool
    }
    
    /// <summary>
    /// Task definition
    /// </summary>
    [Serializable]
    public class TaskDef
    {
        public string Id;
        public string QueueId;
        public string TaskType;
        public SimId TargetEntityId;
        public Vector3 Position;
        public float Priority = 0f; // Higher = more urgent
        
        // Timing
        public float TravelTimeSeconds = 10f; // Worker travel time to task
        public float ProcessTimeSeconds = 5f; // Time to complete task at location
        public float ReturnTimeSeconds = 10f; // Worker return time
        
        // Rewards on completion
        public Dictionary<string, int> CompletionRewards = new();
        
        // Optional metadata
        public Dictionary<string, object> Metadata = new();
    }
    
    /// <summary>
    /// Task instance (runtime state)
    /// </summary>
    [Serializable]
    public class TaskInstance
    {
        public string Id;
        public TaskDef Definition;
        public TaskStatus Status;
        public string AssignedWorkerId;
        public float SubmitTime;
        public float AssignTime;
        public float ArrivalTime;
        public float CompletionTime;
    }
    
    /// <summary>
    /// Worker instance
    /// </summary>
    [Serializable]
    public class WorkerInstance
    {
        public string Id;
        public string PoolId;
        public WorkerStatus Status;
        public string AssignedTaskId;
        public Vector3 Position;
        public Vector3 HomePosition;
        public float StateChangeTime;
    }
    
    /// <summary>
    /// Worker pool configuration
    /// </summary>
    [Serializable]
    public class WorkerPoolDef
    {
        public string Id;
        public string DisplayName;
        public int WorkerCount;
        public Vector3 HomePosition; // Where workers return to
        
        // Speed/timing modifiers
        public float TravelSpeedMultiplier = 1f;
        public float ProcessSpeedMultiplier = 1f;
    }
    
    /// <summary>
    /// Task queue module interface
    /// </summary>
    public interface ITaskQueueModule : ISimModule
    {
        // Queue management
        void RegisterQueue(string queueId, WorkerPoolDef workerPool);
        
        // Task operations
        string SubmitTask(TaskDef task);
        void CancelTask(string taskId);
        
        // Queries
        TaskInstance GetTask(string taskId);
        IEnumerable<TaskInstance> GetPendingTasks(string queueId);
        IEnumerable<TaskInstance> GetActiveTasks(string queueId);
        int GetPendingCount(string queueId);
        int GetAvailableWorkerCount(string queueId);
        
        // Worker queries
        WorkerInstance GetWorker(string workerId);
        IEnumerable<WorkerInstance> GetWorkers(string poolId);
    }
    
    /// <summary>
    /// Task queue module implementation
    /// </summary>
    public class TaskQueueModule : ITaskQueueModule
    {
        private readonly Dictionary<string, WorkerPoolDef> _pools = new();
        private readonly Dictionary<string, List<WorkerInstance>> _workers = new();
        private readonly Dictionary<string, TaskInstance> _tasks = new();
        private readonly Dictionary<string, List<TaskInstance>> _queueTasks = new(); // queueId -> tasks
        
        private int _nextTaskId = 1;
        private int _nextWorkerId = 1;
        
        private SignalBus _signalBus;
        private SimWorld _world;
        
        public TaskQueueModule() { }
        
        #region ISimModule
        
        public void Initialize(SimWorld world)
        {
            _world = world;
            _signalBus = world.SignalBus;
        }
        
        public void Tick(float deltaTime)
        {
            float currentTime = Time.time;
            
            foreach (var pool in _pools.Values)
            {
                TickQueue(pool.Id, currentTime);
            }
        }
        
        public void Shutdown()
        {
            _pools.Clear();
            _workers.Clear();
            _tasks.Clear();
            _queueTasks.Clear();
        }
        
        #endregion
        
        #region Queue Management
        
        public void RegisterQueue(string queueId, WorkerPoolDef workerPool)
        {
            workerPool.Id = queueId;
            _pools[queueId] = workerPool;
            _queueTasks[queueId] = new List<TaskInstance>();
            
            // Create workers
            var workers = new List<WorkerInstance>();
            for (int i = 0; i < workerPool.WorkerCount; i++)
            {
                workers.Add(new WorkerInstance
                {
                    Id = $"{queueId}_worker_{_nextWorkerId++}",
                    PoolId = queueId,
                    Status = WorkerStatus.Available,
                    Position = workerPool.HomePosition,
                    HomePosition = workerPool.HomePosition
                });
            }
            _workers[queueId] = workers;
            
            SimCoreLogger.Log($"[TaskQueueModule] Registered queue '{queueId}' with {workerPool.WorkerCount} workers");
        }
        
        #endregion
        
        #region Task Operations
        
        public string SubmitTask(TaskDef task)
        {
            if (!_pools.ContainsKey(task.QueueId))
            {
                SimCoreLogger.LogWarning($"[TaskQueueModule] Unknown queue: {task.QueueId}");
                return null;
            }
            
            string taskId = $"task_{_nextTaskId++}";
            task.Id = taskId;
            
            var instance = new TaskInstance
            {
                Id = taskId,
                Definition = task,
                Status = TaskStatus.Pending,
                SubmitTime = Time.time
            };
            
            _tasks[taskId] = instance;
            _queueTasks[task.QueueId].Add(instance);
            
            _signalBus?.Publish(new TaskSubmittedSignal
            {
                TaskId = taskId,
                QueueId = task.QueueId,
                TaskType = task.TaskType,
                TargetEntityId = task.TargetEntityId,
                Position = task.Position
            });
            
            SimCoreLogger.Log($"[TaskQueueModule] Task submitted: {taskId} to queue '{task.QueueId}'");
            
            // Try to assign immediately
            TryAssignWorker(task.QueueId, instance);
            
            return taskId;
        }
        
        public void CancelTask(string taskId)
        {
            if (!_tasks.TryGetValue(taskId, out var task)) return;
            if (task.Status == TaskStatus.Completed || task.Status == TaskStatus.Cancelled) return;
            
            // Free worker if assigned
            if (!string.IsNullOrEmpty(task.AssignedWorkerId))
            {
                var worker = GetWorkerById(task.AssignedWorkerId);
                if (worker != null)
                {
                    worker.Status = WorkerStatus.Available;
                    worker.AssignedTaskId = null;
                }
            }
            
            task.Status = TaskStatus.Cancelled;
            
            _signalBus?.Publish(new TaskCancelledSignal
            {
                TaskId = taskId,
                QueueId = task.Definition.QueueId,
                Reason = "Cancelled"
            });
        }
        
        #endregion
        
        #region Queries
        
        public TaskInstance GetTask(string taskId)
        {
            return _tasks.TryGetValue(taskId, out var task) ? task : null;
        }
        
        public IEnumerable<TaskInstance> GetPendingTasks(string queueId)
        {
            if (!_queueTasks.TryGetValue(queueId, out var tasks)) yield break;
            foreach (var task in tasks)
            {
                if (task.Status == TaskStatus.Pending)
                    yield return task;
            }
        }
        
        public IEnumerable<TaskInstance> GetActiveTasks(string queueId)
        {
            if (!_queueTasks.TryGetValue(queueId, out var tasks)) yield break;
            foreach (var task in tasks)
            {
                if (task.Status == TaskStatus.Assigned || task.Status == TaskStatus.InProgress)
                    yield return task;
            }
        }
        
        public int GetPendingCount(string queueId)
        {
            int count = 0;
            foreach (var _ in GetPendingTasks(queueId)) count++;
            return count;
        }
        
        public int GetAvailableWorkerCount(string queueId)
        {
            if (!_workers.TryGetValue(queueId, out var workers)) return 0;
            int count = 0;
            foreach (var w in workers)
            {
                if (w.Status == WorkerStatus.Available) count++;
            }
            return count;
        }
        
        public WorkerInstance GetWorker(string workerId)
        {
            return GetWorkerById(workerId);
        }
        
        public IEnumerable<WorkerInstance> GetWorkers(string poolId)
        {
            if (_workers.TryGetValue(poolId, out var workers))
            {
                foreach (var w in workers) yield return w;
            }
        }
        
        #endregion
        
        #region Internal Logic
        
        private void TickQueue(string queueId, float currentTime)
        {
            if (!_workers.TryGetValue(queueId, out var workers)) return;
            if (!_pools.TryGetValue(queueId, out var pool)) return;
            
            foreach (var worker in workers)
            {
                TickWorker(worker, pool, currentTime);
            }
            
            // Clean up completed/cancelled tasks
            if (_queueTasks.TryGetValue(queueId, out var tasks))
            {
                tasks.RemoveAll(t => t.Status == TaskStatus.Completed || t.Status == TaskStatus.Cancelled);
            }
        }
        
        private void TickWorker(WorkerInstance worker, WorkerPoolDef pool, float currentTime)
        {
            switch (worker.Status)
            {
                case WorkerStatus.Available:
                    // Try to pick up pending task
                    TryAssignPendingTask(worker);
                    break;
                    
                case WorkerStatus.EnRoute:
                    // Check if arrived
                    var task = GetTask(worker.AssignedTaskId);
                    if (task != null)
                    {
                        float travelTime = task.Definition.TravelTimeSeconds / pool.TravelSpeedMultiplier;
                        if (currentTime >= worker.StateChangeTime + travelTime)
                        {
                            WorkerArrived(worker, task, currentTime);
                        }
                    }
                    break;
                    
                case WorkerStatus.Working:
                    // Check if done
                    task = GetTask(worker.AssignedTaskId);
                    if (task != null)
                    {
                        float processTime = task.Definition.ProcessTimeSeconds / pool.ProcessSpeedMultiplier;
                        if (currentTime >= worker.StateChangeTime + processTime)
                        {
                            TaskComplete(worker, task, currentTime);
                        }
                    }
                    break;
                    
                case WorkerStatus.Returning:
                    // Check if returned to base
                    task = GetTask(worker.AssignedTaskId);
                    float returnTime = task?.Definition.ReturnTimeSeconds ?? 5f;
                    returnTime /= pool.TravelSpeedMultiplier;
                    
                    if (currentTime >= worker.StateChangeTime + returnTime)
                    {
                        worker.Status = WorkerStatus.Available;
                        worker.Position = worker.HomePosition;
                        worker.AssignedTaskId = null;
                        
                        SimCoreLogger.Log($"[TaskQueueModule] Worker {worker.Id} returned to base");
                    }
                    break;
            }
        }
        
        private void TryAssignPendingTask(WorkerInstance worker)
        {
            if (!_queueTasks.TryGetValue(worker.PoolId, out var tasks)) return;
            
            // Find highest priority pending task
            TaskInstance bestTask = null;
            float bestPriority = float.MinValue;
            
            foreach (var task in tasks)
            {
                if (task.Status != TaskStatus.Pending) continue;
                if (task.Definition.Priority > bestPriority)
                {
                    bestPriority = task.Definition.Priority;
                    bestTask = task;
                }
            }
            
            if (bestTask != null)
            {
                AssignWorkerToTask(worker, bestTask);
            }
        }
        
        private void TryAssignWorker(string queueId, TaskInstance task)
        {
            if (!_workers.TryGetValue(queueId, out var workers)) return;
            
            // Find available worker (could optimize: find closest)
            foreach (var worker in workers)
            {
                if (worker.Status == WorkerStatus.Available)
                {
                    AssignWorkerToTask(worker, task);
                    return;
                }
            }
            
            // No worker available - task stays pending
            SimCoreLogger.Log($"[TaskQueueModule] No workers available for {task.Id}, task queued");
        }
        
        private void AssignWorkerToTask(WorkerInstance worker, TaskInstance task)
        {
            var pool = _pools[worker.PoolId];
            float travelTime = task.Definition.TravelTimeSeconds / pool.TravelSpeedMultiplier;
            
            worker.Status = WorkerStatus.EnRoute;
            worker.AssignedTaskId = task.Id;
            worker.StateChangeTime = Time.time;
            
            task.Status = TaskStatus.Assigned;
            task.AssignedWorkerId = worker.Id;
            task.AssignTime = Time.time;
            task.ArrivalTime = Time.time + travelTime;
            
            _signalBus?.Publish(new WorkerAssignedSignal
            {
                TaskId = task.Id,
                WorkerId = worker.Id,
                QueueId = task.Definition.QueueId,
                EstimatedArrivalSeconds = travelTime
            });
            
            SimCoreLogger.Log($"[TaskQueueModule] Worker {worker.Id} assigned to {task.Id}, ETA: {travelTime:F1}s");
        }
        
        private void WorkerArrived(WorkerInstance worker, TaskInstance task, float currentTime)
        {
            worker.Status = WorkerStatus.Working;
            worker.Position = task.Definition.Position;
            worker.StateChangeTime = currentTime;
            
            task.Status = TaskStatus.InProgress;
            
            _signalBus?.Publish(new WorkerArrivedSignal
            {
                TaskId = task.Id,
                WorkerId = worker.Id,
                QueueId = task.Definition.QueueId,
                TargetEntityId = task.Definition.TargetEntityId
            });
            
            SimCoreLogger.Log($"[TaskQueueModule] Worker {worker.Id} arrived at {task.Id}");
        }
        
        private void TaskComplete(WorkerInstance worker, TaskInstance task, float currentTime)
        {
            task.Status = TaskStatus.Completed;
            task.CompletionTime = currentTime;
            
            worker.Status = WorkerStatus.Returning;
            worker.StateChangeTime = currentTime;
            
            _signalBus?.Publish(new TaskCompletedSignal
            {
                TaskId = task.Id,
                WorkerId = worker.Id,
                QueueId = task.Definition.QueueId,
                TaskType = task.Definition.TaskType,
                TargetEntityId = task.Definition.TargetEntityId,
                Rewards = task.Definition.CompletionRewards
            });
            
            SimCoreLogger.Log($"[TaskQueueModule] Task {task.Id} completed, worker {worker.Id} returning");
        }
        
        private WorkerInstance GetWorkerById(string workerId)
        {
            foreach (var workers in _workers.Values)
            {
                foreach (var w in workers)
                {
                    if (w.Id == workerId) return w;
                }
            }
            return null;
        }
        
        #endregion
    }
}

