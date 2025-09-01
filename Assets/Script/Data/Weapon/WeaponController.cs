using UnityEngine;

public class WeaponController : MonoBehaviour
{
    // Playerデータを管理する
    WeaponModel model;

    public void Init(WeaponEntity weaponEntity)
    {
        // CardModelを作成し、データを適用
        model = new WeaponModel(weaponEntity);
    }
}
