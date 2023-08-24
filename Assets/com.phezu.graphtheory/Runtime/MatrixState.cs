using UnityEngine;

namespace Phezu.GraphTheory {

    [CreateAssetMenu(fileName = "Matrix State", menuName = "Phezu/GraphTheory/Matrix")]
    public class MatrixState : ScriptableObject {
        public int RowsCount;
        public int ColumnsCount;
        public int[,] Matrix;
    }
}