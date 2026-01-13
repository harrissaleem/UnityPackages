// SimCore - SignalBus Performance Test
// TRUE apples-to-apples comparison with no hidden overhead
// Each test measures the EXACT same thing: cost per function invocation

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using SimCore.Signals;

namespace SimCore.Tests
{
    /// <summary>
    /// Accurate performance test comparing:
    /// - Direct function call (baseline)
    /// - C# Action delegate
    /// - C# Event
    /// - SignalBus
    /// 
    /// All tests use 1 subscriber and measure cost per invocation.
    /// </summary>
    public class SignalBusPerformanceTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [Tooltip("Number of iterations per test")]
        [SerializeField] private int _iterations = 1000000;
        
        [Tooltip("Number of times to repeat each test for statistics")]
        [SerializeField] private int _testRuns = 5;
        
        [SerializeField] private bool _runOnStart = true;
        
        [Header("Results")]
        [SerializeField] private long _directTicks;
        [SerializeField] private long _actionTicks;
        [SerializeField] private long _eventTicks;
        [SerializeField] private long _signalBusSimpleTicks;
        [SerializeField] private long _signalBusComplexTicks;
        
        // Test signals - keep small for fair comparison
        private struct SimpleSignal : ISignal
        {
            public int Value;
        }
        
        private struct ComplexSignal : ISignal
        {
            public int IntValue;
            public float FloatValue;
            public Vector3 Position;
            public Vector3 Rotation;
        }
        
        // Single subscriber for all tests
        private int _callCount;
        private int _lastValue;
        
        // Prevent inlining so we measure actual function call overhead
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void HandleDirect(int value)
        {
            _callCount++;
            _lastValue = value;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void HandleAction(int value)
        {
            _callCount++;
            _lastValue = value;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void HandleSimpleSignal(SimpleSignal signal)
        {
            _callCount++;
            _lastValue = signal.Value;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void HandleComplexSignal(ComplexSignal signal)
        {
            _callCount++;
            _lastValue = signal.IntValue;
        }
        
        private void Start()
        {
            if (_runOnStart)
            {
                Invoke(nameof(RunAllTests), 0.5f);
            }
        }
        
        [ContextMenu("Run Performance Tests")]
        public void RunAllTests()
        {
            UnityEngine.Debug.Log("╔═══════════════════════════════════════════════════════════════════╗");
            UnityEngine.Debug.Log("║         SIGNALBUS PERFORMANCE TEST (TRUE COMPARISON)             ║");
            UnityEngine.Debug.Log("╚═══════════════════════════════════════════════════════════════════╝");
            UnityEngine.Debug.Log($"Iterations per test: {_iterations:N0}");
            UnityEngine.Debug.Log($"Test runs for statistics: {_testRuns}");
            UnityEngine.Debug.Log($"Stopwatch frequency: {Stopwatch.Frequency:N0} ticks/second");
            UnityEngine.Debug.Log("");
            
            // Warmup all code paths
            Warmup();
            
            // Run tests
            var directResult = RunTest("Direct Call", TestDirect);
            var actionResult = RunTest("C# Action", TestAction);
            var eventResult = RunTest("C# Event", TestEvent);
            var signalSimpleResult = RunTest("SignalBus (Simple)", TestSignalBusSimple);
            var signalComplexResult = RunTest("SignalBus (Complex)", TestSignalBusComplex);
            
            // Store for inspector
            _directTicks = directResult.ticks;
            _actionTicks = actionResult.ticks;
            _eventTicks = eventResult.ticks;
            _signalBusSimpleTicks = signalSimpleResult.ticks;
            _signalBusComplexTicks = signalComplexResult.ticks;
            
            // Print summary
            PrintSummary(directResult, actionResult, eventResult, signalSimpleResult, signalComplexResult);
        }
        
        private void Warmup()
        {
            UnityEngine.Debug.Log("Warming up JIT...");
            
            // Setup
            Action<int> action = HandleAction;
            var eventMgr = new EventManager();
            eventMgr.OnValue += HandleAction;
            var signalBus = new SignalBus();
            signalBus.Subscribe<SimpleSignal>(HandleSimpleSignal);
            signalBus.Subscribe<ComplexSignal>(HandleComplexSignal);
            
            // Warm each path
            for (int w = 0; w < 3; w++)
            {
                for (int i = 0; i < 10000; i++)
                {
                    HandleDirect(i);
                    action(i);
                    eventMgr.Raise(i);
                    signalBus.Publish(new SimpleSignal { Value = i });
                    signalBus.Publish(new ComplexSignal { IntValue = i });
                }
            }
            
            _callCount = 0;
            
            // Force GC
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            
            UnityEngine.Debug.Log("Warmup complete.\n");
        }
        
        private (long ticks, double ms, long gcBytes) RunTest(string name, Func<(long ticks, long gcBytes)> testFunc)
        {
            var ticksList = new List<long>(_testRuns);
            var gcList = new List<long>(_testRuns);
            
            for (int run = 0; run < _testRuns; run++)
            {
                // GC before each run
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                
                var result = testFunc();
                ticksList.Add(result.ticks);
                gcList.Add(result.gcBytes);
            }
            
            // Use minimum ticks (best case, least noise)
            ticksList.Sort();
            long minTicks = ticksList[0];
            double ms = (double)minTicks / Stopwatch.Frequency * 1000.0;
            
            // Use max GC (catches any allocations)
            gcList.Sort();
            long maxGc = gcList[gcList.Count - 1];
            
            // Verify: each run adds _iterations calls, so total is _iterations * _testRuns
            int expectedCalls = _iterations * _testRuns;
            if (_callCount != expectedCalls)
            {
                UnityEngine.Debug.LogError($"[{name}] Call count mismatch: expected {expectedCalls}, got {_callCount}");
            }
            _callCount = 0;
            
            // Log
            double nsPerCall = (double)minTicks / _iterations / Stopwatch.Frequency * 1e9;
            string gcStr = maxGc == 0 ? "0 B" : maxGc >= 1024 ? $"{maxGc/1024.0:F1} KB" : $"{maxGc} B";
            string gcColor = maxGc == 0 ? "green" : "red";
            
            UnityEngine.Debug.Log($"<color=cyan>[{name}]</color>");
            UnityEngine.Debug.Log($"  Best time: {ms:F4} ms ({minTicks:N0} ticks)");
            UnityEngine.Debug.Log($"  Per call: {nsPerCall:F2} ns");
            UnityEngine.Debug.Log($"  <color={gcColor}>GC: {gcStr}</color>");
            UnityEngine.Debug.Log("");
            
            return (minTicks, ms, maxGc);
        }
        
        #region Test Methods
        
        private (long ticks, long gcBytes) TestDirect()
        {
            int iterations = _iterations;
            
            // Try no-GC region
            bool noGc = false;
            try { noGc = GC.TryStartNoGCRegion(1024 * 1024, true); } catch { }
            
            long memBefore = GC.GetTotalMemory(false);
            
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                HandleDirect(i);
            }
            
            sw.Stop();
            long memAfter = GC.GetTotalMemory(false);
            
            if (noGc) try { GC.EndNoGCRegion(); } catch { }
            
            return (sw.ElapsedTicks, Math.Max(0, memAfter - memBefore));
        }
        
        private (long ticks, long gcBytes) TestAction()
        {
            Action<int> action = HandleAction;
            int iterations = _iterations;
            
            bool noGc = false;
            try { noGc = GC.TryStartNoGCRegion(1024 * 1024, true); } catch { }
            
            long memBefore = GC.GetTotalMemory(false);
            
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                action(i);
            }
            
            sw.Stop();
            long memAfter = GC.GetTotalMemory(false);
            
            if (noGc) try { GC.EndNoGCRegion(); } catch { }
            
            return (sw.ElapsedTicks, Math.Max(0, memAfter - memBefore));
        }
        
        private (long ticks, long gcBytes) TestEvent()
        {
            var eventMgr = new EventManager();
            eventMgr.OnValue += HandleAction;
            int iterations = _iterations;
            
            bool noGc = false;
            try { noGc = GC.TryStartNoGCRegion(1024 * 1024, true); } catch { }
            
            long memBefore = GC.GetTotalMemory(false);
            
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                eventMgr.Raise(i);
            }
            
            sw.Stop();
            long memAfter = GC.GetTotalMemory(false);
            
            if (noGc) try { GC.EndNoGCRegion(); } catch { }
            
            return (sw.ElapsedTicks, Math.Max(0, memAfter - memBefore));
        }
        
        private (long ticks, long gcBytes) TestSignalBusSimple()
        {
            var signalBus = new SignalBus();
            signalBus.Subscribe<SimpleSignal>(HandleSimpleSignal);
            int iterations = _iterations;
            
            bool noGc = false;
            try { noGc = GC.TryStartNoGCRegion(1024 * 1024, true); } catch { }
            
            long memBefore = GC.GetTotalMemory(false);
            
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                signalBus.Publish(new SimpleSignal { Value = i });
            }
            
            sw.Stop();
            long memAfter = GC.GetTotalMemory(false);
            
            if (noGc) try { GC.EndNoGCRegion(); } catch { }
            
            return (sw.ElapsedTicks, Math.Max(0, memAfter - memBefore));
        }
        
        private (long ticks, long gcBytes) TestSignalBusComplex()
        {
            var signalBus = new SignalBus();
            signalBus.Subscribe<ComplexSignal>(HandleComplexSignal);
            int iterations = _iterations;
            
            bool noGc = false;
            try { noGc = GC.TryStartNoGCRegion(1024 * 1024, true); } catch { }
            
            long memBefore = GC.GetTotalMemory(false);
            
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                signalBus.Publish(new ComplexSignal 
                { 
                    IntValue = i,
                    FloatValue = i * 0.5f,
                    Position = new Vector3(i, i, i),
                    Rotation = new Vector3(i, i, i)
                });
            }
            
            sw.Stop();
            long memAfter = GC.GetTotalMemory(false);
            
            if (noGc) try { GC.EndNoGCRegion(); } catch { }
            
            return (sw.ElapsedTicks, Math.Max(0, memAfter - memBefore));
        }
        
        #endregion
        
        private void PrintSummary(
            (long ticks, double ms, long gcBytes) direct,
            (long ticks, double ms, long gcBytes) action,
            (long ticks, double ms, long gcBytes) evt,
            (long ticks, double ms, long gcBytes) signalSimple,
            (long ticks, double ms, long gcBytes) signalComplex)
        {
            UnityEngine.Debug.Log("╔═══════════════════════════════════════════════════════════════════╗");
            UnityEngine.Debug.Log("║                          SUMMARY                                  ║");
            UnityEngine.Debug.Log("╚═══════════════════════════════════════════════════════════════════╝");
            
            var results = new List<(string name, long ticks, double ms, long gc)>
            {
                ("Direct Call", direct.ticks, direct.ms, direct.gcBytes),
                ("C# Action", action.ticks, action.ms, action.gcBytes),
                ("C# Event", evt.ticks, evt.ms, evt.gcBytes),
                ("SignalBus (Simple)", signalSimple.ticks, signalSimple.ms, signalSimple.gcBytes),
                ("SignalBus (Complex)", signalComplex.ticks, signalComplex.ms, signalComplex.gcBytes)
            };
            
            results.Sort((a, b) => a.ticks.CompareTo(b.ticks));
            
            long baseline = results[0].ticks;
            
            UnityEngine.Debug.Log("<color=white><b>⏱ RANKING (Fastest to Slowest):</b></color>");
            UnityEngine.Debug.Log("");
            
            for (int i = 0; i < results.Count; i++)
            {
                var (name, ticks, ms, gc) = results[i];
                double multiplier = (double)ticks / baseline;
                double nsPerCall = (double)ticks / _iterations / Stopwatch.Frequency * 1e9;
                
                string color = i == 0 ? "green" : i == results.Count - 1 ? "red" : "yellow";
                string gcStr = gc == 0 ? "<color=green>0 B</color>" : $"<color=red>{gc} B</color>";
                
                UnityEngine.Debug.Log($"  {i+1}. <color={color}>{name,-20}</color> {ms,8:F3} ms | {nsPerCall,6:F1} ns/call | {multiplier,5:F2}x | GC: {gcStr}");
            }
            
            UnityEngine.Debug.Log("");
            UnityEngine.Debug.Log("<color=white><b>📊 OVERHEAD vs DIRECT CALL:</b></color>");
            
            double actionOverhead = ((double)action.ticks / direct.ticks - 1) * 100;
            double eventOverhead = ((double)evt.ticks / direct.ticks - 1) * 100;
            double signalSimpleOverhead = ((double)signalSimple.ticks / direct.ticks - 1) * 100;
            double signalComplexOverhead = ((double)signalComplex.ticks / direct.ticks - 1) * 100;
            
            UnityEngine.Debug.Log($"  C# Action:          {actionOverhead,+8:F1}%");
            UnityEngine.Debug.Log($"  C# Event:           {eventOverhead,+8:F1}%");
            UnityEngine.Debug.Log($"  SignalBus (Simple): {signalSimpleOverhead,+8:F1}%");
            UnityEngine.Debug.Log($"  SignalBus (Complex): {signalComplexOverhead,+8:F1}%");
            
            UnityEngine.Debug.Log("");
            UnityEngine.Debug.Log("<color=white><b>📊 OVERHEAD vs C# ACTION:</b></color>");
            
            double signalVsAction = ((double)signalSimple.ticks / action.ticks - 1) * 100;
            UnityEngine.Debug.Log($"  SignalBus (Simple): {signalVsAction,+8:F1}%");
            
            UnityEngine.Debug.Log("");
            UnityEngine.Debug.Log("<color=cyan><b>🎮 REAL-WORLD IMPACT:</b></color>");
            
            double signalNsPerCall = (double)signalSimple.ticks / _iterations / Stopwatch.Frequency * 1e9;
            double directNsPerCall = (double)direct.ticks / _iterations / Stopwatch.Frequency * 1e9;
            
            int[] signalsPerFrame = { 10, 50, 100, 500 };
            foreach (int count in signalsPerFrame)
            {
                double signalMs = signalNsPerCall * count / 1_000_000;
                double directMs = directNsPerCall * count / 1_000_000;
                double overhead = signalMs - directMs;
                double framePercent = signalMs / 16.67 * 100;
                
                string color = framePercent < 1 ? "green" : framePercent < 5 ? "yellow" : "red";
                UnityEngine.Debug.Log($"  {count,3} signals/frame: <color={color}>{signalMs:F4} ms</color> (overhead: {overhead:F4} ms, {framePercent:F2}% of 60fps budget)");
            }
            
            UnityEngine.Debug.Log("");
            UnityEngine.Debug.Log("<color=white><b>💡 VERDICT:</b></color>");
            
            if (signalVsAction < 50)
            {
                UnityEngine.Debug.Log("<color=green>✓ EXCELLENT: SignalBus overhead is minimal. Use freely!</color>");
            }
            else if (signalVsAction < 200)
            {
                UnityEngine.Debug.Log("<color=yellow>⚠ ACCEPTABLE: SignalBus has moderate overhead.</color>");
                UnityEngine.Debug.Log("<color=yellow>   Fine for most cases. Consider direct calls for 500+ signals/frame.</color>");
            }
            else
            {
                UnityEngine.Debug.Log("<color=red>✗ HIGH OVERHEAD: Consider optimization or limiting usage.</color>");
            }
            
            if (signalSimple.gcBytes > 0)
            {
                UnityEngine.Debug.Log($"<color=red>⚠ GC WARNING: SignalBus allocates {signalSimple.gcBytes} bytes!</color>");
            }
            else
            {
                UnityEngine.Debug.Log("<color=green>✓ ZERO GC: No allocations in SignalBus hot path!</color>");
            }
            
            UnityEngine.Debug.Log("");
            UnityEngine.Debug.Log("═══════════════════════════════════════════════════════════════════════");
        }
        
        private class EventManager
        {
            public event Action<int> OnValue;
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Raise(int value) => OnValue?.Invoke(value);
        }
    }
}
