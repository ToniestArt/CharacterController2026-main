using UnityEngine; //Con esto podemos usar el motor de Unity.
using UnityEngine.InputSystem; //Con esto podemos usar el sistema de Input de Unity.
using UnityEngine.SceneManagement; //Con esto podemos usar el SceneManager para cargar escenas.

/*
1. Ponemos el Using UnityEngine.InputSystem; para poder usar el sistema de Input de Unity.
2. Creamos dos variables privadas de tipo InputAction, una para el click y otra para la posición del mouse.
3. Creamos una variable privada de tipo Vector2 para almacenar la posición del mouse.
4. En el método Awake inicializamos las variables de InputAction con las acciones definidas en el Input System.
5. En el método Update verificamos si se ha presionado el click del mouse en este frame, y si es así, llamamos al método ShotRaycast.
6. En el método ShotRaycast leemos la posición del mouse y la almacenamos en la variable _mousePosition.
7. Creamos un rayo desde la cámara principal hacia la posición del mouse usando Camera.main.ScreenPointToRay.
8. Usamos Physics.Raycast para verificar si el rayo colisiona con algún objeto en la escena.
9. Si el rayo colisiona con un objeto, verificamos si el nombre del objeto es "Objeto1", si tiene el tag "Objetos" o si está en la capa 3, y mostramos un mensaje en la consola correspondiente. Además, si el objeto impactado es "Objeto1", cargamos la escena "Escena 1".
*/

public class RaycastRepaso : MonoBehaviour
{
    private InputAction _clickInput;
    private InputAction _positionInput;
    private Vector2 _mousePosition;

    // Awake is called when the script instance is being loaded, aditionaly we can use it to initialize our InputActions.
    void Awake()
    {
        _clickInput = InputSystem.actions["Attack"];
        _positionInput = InputSystem.actions["MousePosition"];
    }

    // Update is called once per frame
    void Update()
    {
        if(_clickInput.WasPressedThisFrame())
        {
            ShotRaycast();
        }
    }

    void ShotRaycast()
    {
        _mousePosition = _positionInput.ReadValue<Vector2>();
        Ray ray = Camera.main.ScreenPointToRay(_mousePosition);
        RaycastHit hit;

        if(Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            if(hit.collider.name == "Objeto1") //Comprobamos si el nombre del objeto impactado es "Objeto1"
            {
                Debug.Log("Has impactado con el objeto 1");
                SceneManager.LoadScene("Escena 1"); //Cargamos la escena "Escena2"
            }

            if(hit.collider.CompareTag("Objetos")) //Comprobamos si el objeto impactado tiene el tag "Objetos"
            {
                Debug.Log("Has impactado con un objeto de tipo Objetos");
            }

            if(hit.collider.gameObject.layer == 3) //Comprobamos si el objeto impactado tiene el componente "Objeto"
            {
                Debug.Log("Has impactado con un objeto en la capa 3");
            }

        }
    }
}
