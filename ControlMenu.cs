
using UnityEngine;
using UnityEngine.SceneManagement;

public class ControlMenu : MonoBehaviour
{
    public void Jugar()
    {
        SceneManager.LoadScene("Juego");
    }
}
