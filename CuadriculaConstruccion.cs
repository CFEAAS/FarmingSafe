using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[System.Serializable]
public class TipoEdificio
{
    public string nombre = "Edificio";
    public GameObject prefab;
    public Sprite icono;
    public int ancho = 2;
    public int largo = 2;
    public float altura = 1f;
    public int costo = 5;
    public int ingreso = 2;
    public float intervaloIngreso = 3f;
}

class EdificioColocado
{
    public TipoEdificio tipo;
    public float temporizador = 0f;
}

public class CuadriculaConstruccion : MonoBehaviour
{
    public static CuadriculaConstruccion Instancia;

    public Terrain terreno;
    public int columnas = 25;
    public int filas = 50;
    public float tamanoCasilla = 1f;
    public Vector3 origen = new Vector3(-13f, 0f, -25f);

    public TipoEdificio[] edificios =
    {
        new TipoEdificio
        {
            nombre = "Casa",
            ancho = 2, largo = 2, altura = 1f,
            costo = 5, ingreso = 2, intervaloIngreso = 3f
        },
        new TipoEdificio
        {
            nombre = "Granero",
            ancho = 2, largo = 3, altura = 1.5f,
            costo = 10, ingreso = 3, intervaloIngreso = 4f
        },
        new TipoEdificio
        {
            nombre = "Molino",
            ancho = 6, largo = 6, altura = 3f,
            costo = 50, ingreso = 10, intervaloIngreso = 5f
        }
    };

    private Camera camara;
    private int columnaActual = -1;
    private int filaActual = -1;
    private bool[,] ocupadas;
    private List<EdificioColocado> colocados = new List<EdificioColocado>();

    private GameObject lineasCuadricula;
    private GameObject resaltado;
    private Material materialResaltado;

    private Color colorValido = new Color(0f, 0.4f, 1f, 0.6f);
    private Color colorInvalido = new Color(1f, 0.1f, 0.1f, 0.6f);

    void Awake()
    {
        Instancia = this;
    }

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

    void Update()
    {
        GenerarIngresos();

        if (camara == null)
        {
            Debug.LogWarning("No se encontro la camara principal.");
            return;
        }

        // Mostrar u ocultar la cuadricula con la tecla X
        if (Input.GetKeyDown(KeyCode.X))
            lineasCuadricula.SetActive(!lineasCuadricula.activeSelf);

        // Saber que edificio esta seleccionado en la interfaz
        int indice = InterfazJuego.Instancia != null
            ? InterfazJuego.Instancia.tipoSeleccionado
            : -1;

        bool construyendo = indice >= 0 && indice < edificios.Length;
        TipoEdificio tipo = construyendo ? edificios[indice] : null;

        int ancho = construyendo ? tipo.ancho : 1;
        int largo = construyendo ? tipo.largo : 1;

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
            if (InterfazJuego.Instancia.PuedePagar(tipo.costo))
            {
                Colocar(tipo, columnaActual, filaActual);
                InterfazJuego.Instancia.Pagar(tipo.costo);
            }
            else
            {
                Debug.Log("No hay dinero suficiente.");
            }
        }
    }

    void GenerarIngresos()
    {
        if (InterfazJuego.Instancia == null)
            return;

        // Cada edificio genera dinero con su propio temporizador
        foreach (EdificioColocado edificio in colocados)
        {
            edificio.temporizador += Time.deltaTime;

            if (edificio.temporizador >= edificio.tipo.intervaloIngreso)
            {
                edificio.temporizador -= edificio.tipo.intervaloIngreso;
                InterfazJuego.Instancia.Ganar(edificio.tipo.ingreso);
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

    void Colocar(TipoEdificio tipo, int columna, int fila)
    {
        Vector3 centro = origen + new Vector3(
            (columna + tipo.ancho / 2f) * tamanoCasilla,
            0f,
            (fila + tipo.largo / 2f) * tamanoCasilla
        );

        if (tipo.prefab != null)
        {
            Instantiate(tipo.prefab, centro, Quaternion.identity);
        }
        else
        {
            // Sin prefab asignado, crear un cubo del tamano del edificio
            GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.transform.localScale = new Vector3(
                tipo.ancho * tamanoCasilla,
                tipo.altura,
                tipo.largo * tamanoCasilla
            );
            centro.y += tipo.altura / 2f;
            cubo.transform.position = centro;
        }

        for (int c = columna; c < columna + tipo.ancho; c++)
        {
            for (int f = fila; f < fila + tipo.largo; f++)
            {
                ocupadas[c, f] = true;
            }
        }

        EdificioColocado nuevo = new EdificioColocado();
        nuevo.tipo = tipo;
        colocados.Add(nuevo);
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