using UnityEngine;

public class LidsController : MonoBehaviour
{
    [Tooltip("Assign 3 lid GameObjects, one for each tube column.")]
    public GameObject[] lids = new GameObject[3];
    public int closeThreshold = 3;

    public void CloseLid(int number)
    {
        if (lids != null && number >= 0 && number < lids.Length && lids[number] != null)
        {
            lids[number].SetActive(true);
        }
    }

    /// <summary>
    /// Opens the lid for the given column (sets it inactive).
    /// </summary>
    public void OpenLid(int number)
    {
        if (lids != null && number >= 0 && number < lids.Length && lids[number] != null)
        {
            lids[number].SetActive(false);
        }
    }

    /// <summary>
    /// Returns true if the lid for the given column is active (closed).
    /// </summary>
    public bool IsLidClosed(int col)
    {
        return lids != null && col >= 0 && col < lids.Length && lids[col] != null && lids[col].activeSelf;
    }

    /// <summary>
    /// Optional: Opens all lids (for reset/cleanup).
    /// </summary>
    public void OpenAllLids()
    {
        if (lids == null) return;
        foreach (var lid in lids)
        {
            if (lid != null) lid.SetActive(false);
        }
    }
}