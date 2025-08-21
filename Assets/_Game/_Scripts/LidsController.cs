using UnityEngine;

public class LidsController : MonoBehaviour
{
    //public TubeManager tubeManager;
    [Tooltip("Assign 3 lid GameObjects, one for each tube column.")]
    public GameObject[] lids = new GameObject[3];
    public int closeThreshold = 3;

    /* public void UpdateLids()
    {
        if (tubeManager == null || lids == null || lids.Length < tubeManager.columns) return;
        var grid = typeof(TubeManager).GetField("grid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(tubeManager) as Ball[,];
        for (int col = 0; col < tubeManager.columns; col++)
        {
            int count = 0;
            for (int row = 0; row < tubeManager.rows; row++)
            {
                if (grid != null && grid[col, row] != null)
                    count++;
            }
            if (lids[col] != null)
                lids[col].SetActive(count >= closeThreshold);
        }
    } */

    public void CloseLid(int number)
    {
        if (lids != null && number >= 0 && number < lids.Length && lids[number] != null)
        {
            lids[number].SetActive(true);
        }
    }
}
