
using UnityEngine;
using UnityEngine.SceneManagement;

public class ControlPausa : MonoBehaviour
{
    public GameObject menuPausa;

    private bool pausado = false;

    void Start()
    {
        Time.timeScale = 1f;
        menuPausa.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pausado)
            {
                Reanudar();
            }
            else
            {
                Pausar();
            }
        }
    }

    public void Pausar()
    {
        pausado = true;
        menuPausa.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Reanudar()
    {
        pausado = false;
        menuPausa.SetActive(false);
        Time.timeScale = 1f;
    }

    public void SalirAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuInicial");
    }

    public void SalirDelJuego()
    {
        Time.timeScale = 1f;
        Application.Quit();
        
    }
}
