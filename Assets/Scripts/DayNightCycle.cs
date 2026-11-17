using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
[SerializeField] int _sunRotationSpeed = 10;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.right * _sunRotationSpeed * Time.deltaTime);
    }       
}
