using UnityEngine;

public class MeleeEnemy : Enemy
{
    Rigidbody _rbody;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        Patrol();
        FindPlayer();

        _maxHealth = 5;
    }

    void ChasePlayer()
    {
        

    }

    public override void Attack ()
    {
        Debug.Log("Void Override Attack Inicializado");
        base.Attack(); //Ejecuta la función Padre sin modificar nada.
        Debug.Log("Void Override Attack Ended");
    }

   
}
