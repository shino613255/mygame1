using TMPro;
using UnityEngine;

public class EnemyValidationHarness : MonoBehaviour
{
    public EnemyData[] enemies;
    public TMP_Text instructions;
    private EnemyManager current;
    private bool showParts = true;

    private void Start() => ShowEnemy(0);

    public void ShowEnemy(int index)
    {
        if (enemies == null || index < 0 || index >= enemies.Length)
        {
            return;
        }

        if (current != null)
        {
            Destroy(current.gameObject);
        }

        current = Instantiate(enemies[index].prefab).GetComponent<EnemyManager>();
        current.Setup(enemies[index]);
        current.SetPartViewVisible(showParts);
        if (instructions != null)
        {
            instructions.text = $"{enemies[index].enemyName}\n1 / 2 / 3：敵切替   V：部位枠線\n部位をクリックして判定を確認";
        }
    }

    private void Update()
    {
        for (int i = 0; i < 3; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                ShowEnemy(i);
            }
        }

        if (current == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            showParts = !showParts;
            current.SetPartViewVisible(showParts);
        }

        if (!Input.GetMouseButtonDown(0) || Camera.main == null)
        {
            return;
        }

        Vector2 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var hit = Physics2D.OverlapPoint(position);
        if (hit == null || !hit.TryGetComponent(out BodyPart part))
        {
            return;
        }

        if (instructions != null)
        {
            instructions.text = $"{current.name}：{part.GetPartNameJP()}（破壊{(part.canBreak ? "可" : "不可")}）\n1 / 2 / 3：敵切替   V：部位枠線";
        }
    }
}
