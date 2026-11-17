using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    public float _maxHealth;
    public float _attackDamage;
    [SerializeField] private PlayerController player;

    private Animator _animator; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("Void Start Inicializado");
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Void Update Inicializada");
    }

    public void FindPlayer()
    {
        Debug.Log("Void Patrol Inicializada");
        
    }

    public void Patrol ()
    {
        Debug.Log("Void Patrol Inicializada");

    }

    public virtual void Attack ()
    {
        //Virtual nos permite polimorfismo (Sobreescritura en la herencia de los childs).
        Debug.Log("Void Attack Inicializada");

    }

    public void TakeDamage ()
    {
        Debug.Log("Void TakeDamage Inicializada");
    }

    public void KillZone ()
    {
        Debug.Log("Void KillZone Inicializada");
        PlayerController player = FindAnyObjectByType<PlayerController>();

        if (player != null)
        {
            player.IsDying();
        }
    }
}
