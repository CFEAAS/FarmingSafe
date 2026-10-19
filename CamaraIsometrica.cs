using UnityEngine;

public class CamaraIsometrica : MonoBehaviour
{
    public float velocidad = 10f;
    public float altura = 12f;
    public float distancia = 12f;
    public float angulo = 65f;
    public float zoom = 12f;

    public float velocidadZoom = 2f;
    public float zoomMinimo = 5f;
    public float zoomMaximo = 20f;

    public float velocidadRotacion = 5f;
    public float anguloMinimo = 20f;
    public float anguloMaximo = 90f;

    private Camera camara;

    void Start()
    {
        camara = GetComponent<Camera>();

        // Configurar la proyeccion ortografica
        camara.orthographic = true;
        camara.orthographicSize = zoom;

        // Posicionar e inclinar la camara
        transform.position = new Vector3(0, altura, -distancia);
        transform.rotation = Quaternion.Euler(angulo, 0, 0);
    }

    void Update()
    {
        // Obtener las entradas del teclado
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A))
            horizontal = -1f;

        if (Input.GetKey(KeyCode.D))
            horizontal = 1f;

        if (Input.GetKey(KeyCode.S))
            vertical = -1f;

        if (Input.GetKey(KeyCode.W))
            vertical = 1f;

        // Calcular las direcciones sobre el plano del suelo
        Vector3 derecha = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * Vector3.right;
        Vector3 adelante = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * Vector3.forward;

        Vector3 movimiento =
            derecha * horizontal + adelante * vertical;

        // Evitar que la diagonal sea mas rapida
        movimiento = Vector3.ClampMagnitude(movimiento, 1f);

        // Mover la camara independientemente de los FPS
        transform.position +=
            movimiento * velocidad * Time.deltaTime;

        // Ajustar el zoom con la rueda del mouse
        float rueda = Input.mouseScrollDelta.y;

        if (rueda != 0f)
        {
            // Cambiar el angulo al mantener presionada la rueda
            if (Input.GetMouseButton(2))
            {
                Plane suelo = new Plane(Vector3.up, Vector3.zero);
                Ray rayo = new Ray(transform.position, transform.forward);

                if (suelo.Raycast(rayo, out float distanciaFoco))
                {
                    Vector3 foco = rayo.GetPoint(distanciaFoco);

                    angulo -= rueda * velocidadRotacion;
                    angulo = Mathf.Clamp(
                        angulo,
                        anguloMinimo,
                        anguloMaximo
                    );

                    transform.rotation = Quaternion.Euler(angulo, 0f, 0f);
                    transform.position = foco - transform.forward * distanciaFoco;
                }
            }
            else
            {
                zoom -= rueda * velocidadZoom;
                zoom = Mathf.Clamp(zoom, zoomMinimo, zoomMaximo);

                camara.orthographicSize = zoom;
            }
        }
    }
}