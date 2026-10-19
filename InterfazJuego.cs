using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InterfazJuego : MonoBehaviour
{
    public static InterfazJuego Instancia;

    public int dinero = 5;
    public float alturaPanel = 200f;
    public float velocidadAnimacion = 10f;

    [HideInInspector]
    public int tipoSeleccionado = -1;

    private TextMeshProUGUI textoDinero;
    private TextMeshProUGUI textoBoton;
    private RectTransform panelInferior;
    private Image[] imagenesBotones;
    private Color[] coloresNormales;
    private bool desplegado = true;

    private Color colorSeleccionado = new Color(1f, 0.85f, 0.2f);

    void Awake()
    {
        Instancia = this;
    }

    void Start()
    {
        CrearPanelDinero();
        panelInferior = CrearPanelInferior();

        ActualizarDinero();
    }

    void Update()
    {
        // Mover la pestana suavemente hacia su posicion destino
        float destino = desplegado ? 0f : -alturaPanel;

        Vector2 posicion = panelInferior.anchoredPosition;
        posicion.y = Mathf.Lerp(
            posicion.y,
            destino,
            velocidadAnimacion * Time.unscaledDeltaTime
        );
        panelInferior.anchoredPosition = posicion;
    }

    public bool PuedePagar(int costo)
    {
        return dinero >= costo;
    }

    public void Pagar(int costo)
    {
        dinero -= costo;
        ActualizarDinero();
    }

    public void Ganar(int cantidad)
    {
        dinero += cantidad;
        ActualizarDinero();
    }

    void ActualizarDinero()
    {
        textoDinero.text = "$ " + dinero;
    }

    void Alternar()
    {
        desplegado = !desplegado;
        textoBoton.text = desplegado ? "Ocultar" : "Mostrar";
    }

    void Seleccionar(int indice)
    {
        // Presionar el mismo boton otra vez quita la seleccion
        tipoSeleccionado = (tipoSeleccionado == indice) ? -1 : indice;

        for (int i = 0; i < imagenesBotones.Length; i++)
        {
            imagenesBotones[i].color = (i == tipoSeleccionado)
                ? colorSeleccionado
                : coloresNormales[i];
        }
    }

    RectTransform CrearPanelDinero()
    {
        RectTransform panel = CrearPanel(
            "PanelDinero",
            transform,
            new Color(0f, 0f, 0f, 0.7f)
        );

        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.zero;
        panel.pivot = Vector2.zero;
        panel.anchoredPosition = new Vector2(10f, 10f);
        panel.sizeDelta = new Vector2(220f, 60f);

        textoDinero = CrearTexto("TextoDinero", panel, "", 32f);

        return panel;
    }

    RectTransform CrearPanelInferior()
    {
        RectTransform panel = CrearPanel(
            "PanelInferior",
            transform,
            new Color(0.1f, 0.1f, 0.1f, 0.85f)
        );

        // Ocupa todo el ancho, dejando libre el espacio del panel de dinero
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(1f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.offsetMin = new Vector2(250f, 0f);
        panel.offsetMax = new Vector2(-10f, alturaPanel);

        // Boton para desplegar o minimizar, pegado al borde superior
        RectTransform boton = CrearPanel(
            "BotonPestana",
            panel,
            new Color(0.1f, 0.1f, 0.1f, 0.85f)
        );

        boton.anchorMin = new Vector2(0.5f, 1f);
        boton.anchorMax = new Vector2(0.5f, 1f);
        boton.pivot = new Vector2(0.5f, 0f);
        boton.anchoredPosition = Vector2.zero;
        boton.sizeDelta = new Vector2(140f, 30f);

        Button componenteBoton = boton.gameObject.AddComponent<Button>();
        componenteBoton.targetGraphic = boton.GetComponent<Image>();
        componenteBoton.onClick.AddListener(Alternar);

        textoBoton = CrearTexto("TextoBoton", boton, "Ocultar", 18f);

        CrearBotonesEdificios(panel);

        return panel;
    }

    void CrearBotonesEdificios(RectTransform panel)
    {
        if (CuadriculaConstruccion.Instancia == null)
        {
            Debug.LogWarning("No se encontro CuadriculaConstruccion.");
            imagenesBotones = new Image[0];
            coloresNormales = new Color[0];
            return;
        }

        TipoEdificio[] tipos = CuadriculaConstruccion.Instancia.edificios;
        imagenesBotones = new Image[tipos.Length];
        coloresNormales = new Color[tipos.Length];

        for (int i = 0; i < tipos.Length; i++)
        {
            int indice = i;
            TipoEdificio tipo = tipos[i];

            RectTransform boton = CrearPanel(
                "BotonEdificio" + i,
                panel,
                Color.white
            );

            boton.anchorMin = new Vector2(0f, 1f);
            boton.anchorMax = new Vector2(0f, 1f);
            boton.pivot = new Vector2(0f, 1f);
            boton.anchoredPosition = new Vector2(20f + i * 135f, -20f);
            boton.sizeDelta = new Vector2(120f, 120f);

            Image imagen = boton.GetComponent<Image>();

            if (tipo.icono != null)
            {
                imagen.sprite = tipo.icono;
                coloresNormales[i] = Color.white;
            }
            else
            {
                // Sin foto asignada, mostrar un cuadro gris con nombre y costo
                coloresNormales[i] = new Color(0.35f, 0.35f, 0.35f);
                CrearTexto(
                    "TextoEdificio",
                    boton,
                    tipo.nombre + "\n$" + tipo.costo,
                    20f
                );
            }

            imagen.color = coloresNormales[i];
            imagenesBotones[i] = imagen;

            Button componenteBoton = boton.gameObject.AddComponent<Button>();
            componenteBoton.transition = Selectable.Transition.None;
            componenteBoton.targetGraphic = imagen;
            componenteBoton.onClick.AddListener(() => Seleccionar(indice));
        }
    }

    RectTransform CrearPanel(string nombre, Transform padre, Color color)
    {
        GameObject objeto = new GameObject(
            nombre,
            typeof(RectTransform),
            typeof(Image)
        );
        objeto.transform.SetParent(padre, false);
        objeto.GetComponent<Image>().color = color;

        return objeto.GetComponent<RectTransform>();
    }

    TextMeshProUGUI CrearTexto(
        string nombre,
        Transform padre,
        string contenido,
        float tamano)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.transform.SetParent(padre, false);

        TextMeshProUGUI texto = objeto.AddComponent<TextMeshProUGUI>();
        texto.text = contenido;
        texto.fontSize = tamano;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = Color.white;
        texto.raycastTarget = false;

        RectTransform rect = texto.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return texto;
    }
}