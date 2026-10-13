using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{

    public void StartGame()
        {
            Debug.Log("Start button clicked - loading scene 1");
            SceneManager.LoadScene(1);
        }


}
