using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Phezu.Util;

namespace Phezu.GraphTheory.Editor {

    public class AdjacencyMatrixSorter : EditorWindow {
        private const string DEFAULT_MEMORY_LOCATION = "com.phezu.graphtheory/ExampleStates";
        private const int MAX_VERTICES = 10;
        private const int MAX_DUPLICATE_EDGES = 1;

        private int m_VertexCount;
        private int[,] m_Matrix;
        private bool m_StatesFoldout;

        private string[] m_Indicies;
        private int[] m_IndiciesInt;
        private int m_SelectedIndexA;
        private int m_SelectedIndexB;

        private string m_StatesLocation;
        private string m_NewStateName;

        private int[] m_Mapping;

        private List<MatrixState> m_SavedStates;

        [MenuItem("Tools/Phezu/GraphTheory/AdjacencyMatrixSorter")]
        public static void ShowWindow() {
            GetWindow(typeof(AdjacencyMatrixSorter));
        }

        private void OnEnable() {
            m_VertexCount = 3;
            m_Matrix = new int[m_VertexCount, m_VertexCount];
            m_StatesLocation = DEFAULT_MEMORY_LOCATION;

            FetchStates();
            OnVertexCountChanged();
        }

        private void InitializeMappingAsIdentity() {
            m_Mapping = new int[m_VertexCount];
            for (int i = 0; i < m_VertexCount; i++)
                m_Mapping[i] = i;
        }

        private void FetchStates() {
            m_SavedStates = FEditor.FindAssetsByType<MatrixState>();
        }

        private void SaveState() {
            if (string.IsNullOrWhiteSpace(m_NewStateName))
                return;

            var newState = FEditor.CreateAsset<MatrixState>(m_StatesLocation + "/" + m_NewStateName + ".asset");
            newState.RowsCount = newState.ColumnsCount = m_VertexCount;
            newState.Matrix = (int[,])m_Matrix.Clone();

            FetchStates();
        }

        private void RemoveDeletedStates() {
            for (int i = 0; i < m_SavedStates.Count; i++) {
                if (m_SavedStates[i] == null)
                    m_SavedStates.RemoveAt(i--);
            }
        }

        private void LoadState(MatrixState state) {
            if (state == null || state.RowsCount <= 0 || state.Matrix == null) {
                Debug.Log(state.ToString() + " is faulty. Removing it");
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(state));
                return;
            }

            m_VertexCount = state.RowsCount;
            m_Matrix = (int[,])state.Matrix.Clone();
        }

        private void CopyMatrixData(int[,] dest, int[,] src, int sizeToCopy) {
            for (int i = 0; i < sizeToCopy; i++)
                for (int j = 0; j < sizeToCopy; j++)
                    dest[i, j] = src[i, j];
        }

        private void IncrementVertexCount() {
            if (m_VertexCount == MAX_VERTICES)
                return;

            var clone = (int[,])m_Matrix.Clone();
            m_Matrix = new int[m_VertexCount + 1, m_VertexCount + 1];
            CopyMatrixData(m_Matrix, clone, m_VertexCount);
            m_VertexCount++;
            OnVertexCountChanged();
        }

        private void DecrementVertexCount() {
            if (m_VertexCount == 1)
                return;

            var clone = (int[,])m_Matrix.Clone();
            m_Matrix = new int[m_VertexCount - 1, m_VertexCount - 1];
            CopyMatrixData(m_Matrix, clone, m_VertexCount - 1);
            m_VertexCount--;
            OnVertexCountChanged();
        }

        private void OnVertexCountChanged() {
            m_Indicies = new string[m_VertexCount];
            m_IndiciesInt = new int[m_VertexCount];

            for (int i = 0; i < m_VertexCount; i++) {
                m_Indicies[i] = "" + i;
                m_IndiciesInt[i] = i;
            }

            InitializeMappingAsIdentity();
        }

        private void PreDraw() {
            RemoveDeletedStates();
        }

        private void OnGUI() {
            PreDraw();

            DrawVertexCounter();

            float y = DrawMatrix();

            y = DrawActions(y);

            DrawStates(y);
        }




        private void RandomizeMatrix() {
            byte[] bytes = BitConverter.GetBytes(EditorApplication.timeSinceStartup);
            UnityEngine.Random.InitState(BitConverter.ToInt32(bytes));

            for (int i = 0; i < m_VertexCount; i++)
                for (int j = 0; j < m_VertexCount; j++)
                    m_Matrix[i, j] = UnityEngine.Random.Range(0, MAX_DUPLICATE_EDGES + 1);
        }

        private void SetOrderAsBase() {
            InitializeMappingAsIdentity();
        }

        private void BruteForceSolve() {

        }

        private void SwapIndicies() {
            if (m_SelectedIndexA == m_SelectedIndexB)
                return;

            int indexA, indexB;
            indexA = m_Mapping.ToList().IndexOf(m_SelectedIndexA);
            indexB = m_Mapping.ToList().IndexOf(m_SelectedIndexB);

            int temp = m_Mapping[indexA];
            m_Mapping[indexA] = m_Mapping[indexB];
            m_Mapping[indexB] = temp;
        }



        #region Drawing

        private void DrawVertexCounter() {
            Rect pos = new Rect(0, 0, 20f, 20f);
            pos.x = position.width / 3f;
            pos.y = 10f;

            if (GUI.Button(pos, "-"))
                DecrementVertexCount();

            GUI.Label(new Rect((position.width / 2f) - 30f, 10f, 100f, 20f), "Vertex Count");

            pos.x = 2 * position.width / 3f;
            if (GUI.Button(pos, "+"))
                IncrementVertexCount();
        }

        private float DrawMatrix() {
            float gridCellWidth = position.width / (m_VertexCount + 1);
            float gridCellHeight = 35f;

            Rect pos = new(gridCellWidth, 30f, gridCellWidth, gridCellHeight);

            for (int i = 0; i < m_VertexCount; i++) {
                GUI.Label(pos, "" + m_Mapping[i]);
                pos.x += gridCellWidth;
            }
            pos.y += gridCellHeight;
            pos.x = gridCellWidth / 1.2f;

            for (int i = 0; i < m_VertexCount; i++) {
                pos.y -= gridCellHeight / 5f;
                pos.x -= gridCellWidth / 2f;
                GUI.Label(pos, "" + m_Mapping[i]);
                pos.x += gridCellWidth / 2f;
                pos.y += gridCellHeight / 5f;

                for (int j = 0; j < m_VertexCount; j++) {
                    Rect matrixElementRect = pos;
                    matrixElementRect.width /= 2f;
                    matrixElementRect.height /= 2f;

                    int Fi = m_Mapping[i], Fj = m_Mapping[j];
                    m_Matrix[Fi, Fj] = EditorGUI.IntField(matrixElementRect, m_Matrix[Fi, Fj]);
                    pos.x += gridCellWidth;
                }
                pos.y += gridCellHeight;
                pos.x = gridCellWidth / 1.2f;
            }

            return pos.y;
        }

        private float DrawActions(float y) {
            float buttonWidth = position.width / 4f;
            Rect rect = new(buttonWidth / 2f, y, buttonWidth, 30f);

            if (GUI.Button(rect, "Randomize"))
                RandomizeMatrix();
            rect.x += buttonWidth;
            if (GUI.Button(rect, "Brute Force"))
                BruteForceSolve();
            rect.x += buttonWidth;
            if (GUI.Button(rect, "Base Order"))
                SetOrderAsBase();

            rect.width /= 2f;
            rect.y += 40f;
            rect.x = buttonWidth / 2f;
            if (GUI.Button(rect, "Swap"))
                SwapIndicies();

            rect.x += rect.width + 10f;
            rect.width = 30f;
            rect.y += 5f;

            m_SelectedIndexA = EditorGUI.IntPopup(rect, m_SelectedIndexA, m_Indicies, m_IndiciesInt);
            rect.x += rect.width + 10f;
            m_SelectedIndexB = EditorGUI.IntPopup(rect, m_SelectedIndexB, m_Indicies, m_IndiciesInt);

            return rect.y;
        }

        private void DrawStates(float y) {
            float elementWidth = position.width / 4f;

            Rect rect = new(elementWidth / 2f, y + 40f, elementWidth, 20f);

            GUI.Button(rect, "Memory Location:");

            rect.y += 25f;
            if (GUI.Button(rect, "Save:"))
                SaveState();
            rect.y -= 25f;

            Rect textRect = rect;
            textRect.x += elementWidth + 10f;
            textRect.width = 2 * elementWidth;

            m_StatesLocation = EditorGUI.TextField(textRect, "", m_StatesLocation);
            textRect.y += 25f;
            m_NewStateName = EditorGUI.TextField(textRect, "", m_NewStateName);

            bool prevFoldoutState = m_StatesFoldout;

            rect.y += 50f;
            m_StatesFoldout = EditorGUI.Foldout(rect, m_StatesFoldout, "Memory");
            rect.y += 25f;
            rect.width = 2 * elementWidth;
            rect.x += 15f;

            if (!m_StatesFoldout)
                return;

            if (prevFoldoutState != m_StatesFoldout)
                FetchStates();

            for (int i = 0; i < m_SavedStates.Count; i++) {
                if (GUI.Button(rect, m_SavedStates[i].name))
                    LoadState(m_SavedStates[i]);
                rect.y += 25;
            }
        }

        #endregion

    }
}