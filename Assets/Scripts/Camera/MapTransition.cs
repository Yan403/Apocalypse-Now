using UnityEngine;
using Unity.Cinemachine;

public class MapTransition : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] PolygonCollider2D MapBoundary;
    [SerializeField] Direction direction;
    [SerializeField] float aditivePos = 2f;

    enum Direction { Up, Down, Left, Right };

    CinemachineConfiner2D confiner;
    private void Awake()
    {
        confiner = FindFirstObjectByType<CinemachineConfiner2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            confiner.BoundingShape2D = MapBoundary;
            UpdatePlayerPosition(collision.gameObject);
        }
    }
    private void UpdatePlayerPosition(GameObject player)
    {
        Vector3 newPos= player.transform.position;

        switch (direction) {
            case Direction.Up:
                newPos.y += aditivePos;
                break;
            case Direction.Down:
                newPos.y -= aditivePos;
                break;
            case Direction.Left:
                newPos.x += aditivePos;
                break;
            case Direction.Right:
                newPos.x -= aditivePos;
                break;
        }

        player.transform.position = newPos;
    }
}   
