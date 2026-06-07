using UnityEngine;

public class MothFloat : MonoBehaviour
{
    Vector3 startPos;
    public float xAmount = 0.3f;
    public float yAmount = 0.15f;
    public float speed = 1f;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float x = Mathf.Sin(Time.time * speed) * xAmount;
        float y = Mathf.Sin(Time.time * speed * 1.7f) * yAmount;

        transform.position = startPos + new Vector3(x, y, 0);
    }
}