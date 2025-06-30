using UnityEngine;

public class WeaponController : MonoBehaviour
{
    // Playerデータを管理する
    WeaponModel model;

    private void Awake()
    {

    }

    public void Init(int WeaponID)
    {
        // PlayerModelを作成し、データを適用
        model = new WeaponModel(WeaponID);
    }
}
