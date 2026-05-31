using UnityEngine;

public class BrickSpawner : MonoBehaviour
{
    [Header("物件設定")]
    public GameObject brickPrefab; // 放入剛剛做好的 Brick Prefab
    public Transform centerEyeAnchor; // 放入 OVRCameraRig 的 CenterEyeAnchor

    [Header("隨機尺寸範圍")]
    public float minSize = 0.2f;
    public float maxSize = 1.2f;

    // 這個方法用來綁定按鈕的 OnClick 事件
    public void SpawnBrickInFrontOfUser()
    {
        if (brickPrefab == null)
        {
            Debug.LogError("請先指派 Brick Prefab！");
            return;
        }

        // 如果沒有手動指派相機，預設抓取主相機
        Transform cameraTransform = centerEyeAnchor != null ? centerEyeAnchor : Camera.main.transform;

        // 計算生成位置：前方 1m (+cameraTransform.forward)，上方 2m (+cameraTransform.up)
        Vector3 spawnPosition = cameraTransform.position + (cameraTransform.forward * 1.0f) + (cameraTransform.up * 2.0f);

        // 隨機旋轉角度，讓形狀看起來更不規則
        Quaternion spawnRotation = Random.rotation;

        // 生成物件
        GameObject newBrick = Instantiate(brickPrefab, spawnPosition, spawnRotation);

        // 隨機一個基礎大小 (例如 0.3m 到 0.5m 之間)
        float baseScale = Random.Range(0.3f, 0.5f);

        // 磚頭通常長寬高比例不同，我們讓 X, Y, Z 在基礎大小周圍做小幅度的規則隨機
        float randomX = baseScale * Random.Range(1.8f, 2.2f); // 變長 (長度)
        float randomY = baseScale * Random.Range(0.8f, 1.2f); // 變矮 (高度)
        float randomZ = baseScale * Random.Range(1.0f, 1.4f); // 變寬 (寬度)
        newBrick.transform.localScale = new Vector3(randomX, randomY, randomZ);
        
        Debug.Log($"已生成隨機 Brick 於位置: {spawnPosition}，尺寸: {newBrick.transform.localScale}");
    }
}