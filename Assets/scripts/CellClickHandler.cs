using UnityEngine;

public enum Owner { None, Player1, Player2 }

public class CellClickHandler : MonoBehaviour
{
    private bool isAlive = false;
    private Owner owner = Owner.None;
    private GridManager gridManager;

    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();
    }

    public void ToggleCell(Owner currentOwner)
    {
        if (!isAlive && gridManager.IsPlacementPhase())
        {
            isAlive = true;
            owner = currentOwner;
            UpdateColor();
        }
    }

    public void SetAliveWithOwner(bool alive, Owner newOwner)
    {
        isAlive = alive;
        owner = alive ? newOwner : Owner.None;
        UpdateColor();
    }

    public bool IsAlive() => isAlive;
    public Owner GetOwner() => owner;
    public void ResetCell()
    {
        isAlive = false;
        owner = Owner.None;
        UpdateColor();
    }

    private void UpdateColor()
    {
        if (!isAlive)
            GetComponent<SpriteRenderer>().color = Color.white;
        else if (owner == Owner.Player1)
            GetComponent<SpriteRenderer>().color = Color.green;
        else if (owner == Owner.Player2)
            GetComponent<SpriteRenderer>().color = Color.red;
    }
}
