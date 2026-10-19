using UnityEngine;
using UnityEngine.EventSystems;

public class CuadriculaConstruccion : MonoBehaviour
{
    public Terrain terreno;
    public int columnas = 25;
    public int filas = 50;
    public float tamanoCasilla = 1f;
    public Vector3 origen = new Vector3(-13f, 0f, -25f);

    public GameObject prefabEdificio;
    public int anchoEdificio = 2;
    public int largoEdificio = 2;
    public float alturaEdificio = 1f;
    public int costoEdificio = 5;

    private Camera camara;
    private int columnaActual = -1;
    private int filaActual = -1;
    private bool[,] ocupadas;

    private GameObject lineasCuadricula;
    private GameObject resaltado;
    private Material materialResaltado;

    private Color colorValido = new Color(0f, 0.4f, 1f, 0.6f);
    private Color colorInvalido = new Color(1f, 0.1f, 0.1f, 0.6f);

    public int ingresoPorEdificio = 2;
    public float intervaloIngreso = 3f;

    private int edificiosColocados = 0;
    private float temporizador = 0f;
    void Start()
    {
        camara = Camera.main;

        // Si hay un terreno asignado, tomar su posicion y dimensiones
        if (terreno != null)
        {
            origen = terreno.transform.position;
            columnas = Mathf.RoundToInt(
                terreno.terrainData.size.x / tamanoCasilla
            );
            filas = Mathf.RoundToInt(
                terreno.terrainData.size.z / tamanoCasilla
            );
        }

        ocupadas = new bool[columnas, filas];

        CrearLineas();
        CrearResaltado();
    }
    void GenerarIngresos()
    {
        if (edificiosColocados == 0 || InterfazJuego.Instancia == null)
            return;

        temporizador += Time.deltaTime;

        if (temporizador >= intervaloIngreso)
        {
            temporizador -= intervaloIngreso;
            InterfazJuego.Instancia.Ganar(
                ingresoPorEdificio * edificiosColocados
            );
        }
    }
    void Update()
    {
        GenerarIngresos();

        if (camara == null)
        {
            Debug.LogWarning("No se encontro la camara principal.");
            return;
        }
        if (camara == null)
        {
            Debug.LogWarning("No se encontro la camara principal.");
            return;
        }

        // Mostrar u ocultar la cuadricula con la tecla X
        if (Input.GetKeyDown(KeyCode.X))
            lineasCuadricula.SetActive(!lineasCuadricula.activeSelf);

        // Saber si hay un edificio seleccionado en la interfaz
        bool construyendo =
            InterfazJuego.Instancia != null &&
            InterfazJuego.Instancia.edificioSeleccionado;

        int ancho = construyendo ? anchoEdificio : 1;
        int largo = construyendo ? largoEdificio : 1;

        Ray rayo = camara.ScreenPointToRay(Input.mousePosition);
        Plane suelo = new Plane(Vector3.up, origen);

        bool hayCasilla = false;
        columnaActual = -1;
        filaActual = -1;

        if (suelo.Raycast(rayo, out float distancia))
        {
            Vector3 posicion = rayo.GetPoint(distancia) - origen;

            int columnaMouse = Mathf.FloorToInt(
                posicion.x / tamanoCasilla
            );

            int filaMouse = Mathf.FloorToInt(
                posicion.z / tamanoCasilla
            );

            // El mouse debe estar dentro de la cuadricula
            if (columnaMouse >= 0 && columnaMouse < columnas &&
                filaMouse >= 0 && filaMouse < filas)
            {
                hayCasilla = true;

                // Centrar el edificio en el mouse
                columnaActual = Mathf.FloorToInt(
                    posicion.x / tamanoCasilla - ancho / 2f + 0.5f
                );

                filaActual = Mathf.FloorToInt(
                    posicion.z / tamanoCasilla - largo / 2f + 0.5f
                );
            }
        }

        bool valida = hayCasilla &&
            EstaLibre(columnaActual, filaActual, ancho, largo);

        // Mover el resaltado a la zona que ocuparia el edificio
        if (hayCasilla)
        {
            resaltado.SetActive(true);
            resaltado.transform.position = origen + new Vector3(
                (columnaActual + ancho / 2f) * tamanoCasilla,
                0.02f,
                (filaActual + largo / 2f) * tamanoCasilla
            );
            resaltado.transform.localScale = new Vector3(
                ancho * tamanoCasilla,
                largo * tamanoCasilla,
                1f
            );
            materialResaltado.color = valida ? colorValido : colorInvalido;
        }
        else
        {
            resaltado.SetActive(false);
        }

        // Colocar el edificio con clic izquierdo
        if (Input.GetMouseButtonDown(0) &&
            construyendo &&
            valida &&
            !EventSystem.current.IsPointerOverGameObject())
        {
            if (InterfazJuego.Instancia.PuedePagar(costoEdificio))
            {
                Colocar(columnaActual, filaActual);
                InterfazJuego.Instancia.Pagar(costoEdificio);
            }
            else
            {
                Debug.Log("No hay dinero suficiente.");
            }
        }
    }

    bool EstaLibre(int columna, int fila, int ancho, int largo)
    {
        if (columna < 0 || fila < 0 ||
            columna + ancho > columnas ||
            fila + largo > filas)
            return false;

        for (int c = columna; c < columna + ancho; c++)
        {
            for (int f = fila; f < fila + largo; f++)
            {
                if (ocupadas[c, f])
                    return false;
            }
        }

        return true;
    }

    void Colocar(int columna, int fila)
    {
        Vector3 centro = origen + new Vector3(
            (columna + anchoEdificio / 2f) * tamanoCasilla,
            0f,
            (fila + largoEdificio / 2f) * tamanoCasilla
        );

        if (prefabEdificio != null)
        {
            Instantiate(prefabEdificio, centro, Quaternion.identity);
        }
        else
        {
            // Sin prefab asignado, crear un cubo del tamano del edificio
            GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.transform.localScale = new Vector3(
                anchoEdificio * tamanoCasilla,
                alturaEdificio,
                largoEdificio * tamanoCasilla
            );
            centro.y += alturaEdificio / 2f;
            cubo.transform.position = centro;
        }

        for (int c = columna; c < columna + anchoEdificio; c++)
        {
            for (int f = fila; f < fila + largoEdificio; f++)
            {
                ocupadas[c, f] = true;
            }
            edificiosColocados++;
        }
    }

    void CrearLineas()
    {
        lineasCuadricula = new GameObject("LineasCuadricula");
        lineasCuadricula.transform.SetParent(transform);

        Material material = new Material(Shader.Find("Sprites/Default"));

        for (int i = 0; i <= columnas; i++)
        {
            float x = i * tamanoCasilla;

            CrearLinea(
                origen + new Vector3(x, 0.03f, 0f),
                origen + new Vector3(x, 0.03f, filas * tamanoCasilla),
                material
            );
        }

        for (int i = 0; i <= filas; i++)
        {
            float z = i * tamanoCasilla;

            CrearLinea(
                origen + new Vector3(0f, 0.03f, z),
                origen + new Vector3(columnas * tamanoCasilla, 0.03f, z),
                material
            );
        }

        // La cuadricula empieza oculta
        lineasCuadricula.SetActive(false);
    }

    void CrearLinea(Vector3 inicio, Vector3 fin, Material material)
    {
        GameObject objeto = new GameObject("Linea");
        objeto.transform.SetParent(lineasCuadricula.transform);

        LineRenderer linea = objeto.AddComponent<LineRenderer>();
        linea.useWorldSpace = true;
        linea.positionCount = 2;
        linea.SetPosition(0, inicio);
        linea.SetPosition(1, fin);
        linea.startWidth = 0.05f;
        linea.endWidth = 0.05f;
        linea.material = material;
        linea.startColor = Color.green;
        linea.endColor = Color.green;
    }

    void CrearResaltado()
    {
        resaltado = GameObject.CreatePrimitive(PrimitiveType.Quad);
        resaltado.name = "CasillaResaltada";
        resaltado.transform.SetParent(transform);

        // Acostar el quad sobre el suelo
        resaltado.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Quitar el collider para que no estorbe a otros raycasts
        Destroy(resaltado.GetComponent<Collider>());

        materialResaltado = new Material(Shader.Find("Sprites/Default"));
        materialResaltado.color = colorValido;
        resaltado.GetComponent<MeshRenderer>().material = materialResaltado;

        resaltado.SetActive(false);
    }
}