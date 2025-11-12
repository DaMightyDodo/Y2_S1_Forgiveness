using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadManager : MonoBehaviour
{
    private string path;
    public TMP_InputField inputField;
    public string username;
    private void Start()
    {
        path = Path.Combine(Application.persistentDataPath, "SaveData.txt");

        Debug.Log(Application.persistentDataPath);
        
        //HasKey = the save exists or not
        //GetInt = Get value with the key (int type)
        //GetFloat
        //GetString
        //check if save game exists or not
        if (PlayerPrefs.HasKey("HighScore"))
        {
            Debug.Log("Player's HighScore is: " + PlayerPrefs.GetInt("HighScore"));
        }
        //if not exists
        else
        {
            //Set a new Highscore value
            PlayerPrefs.SetInt("HighScore", 10);
            Debug.Log("Player's new HighScore is: " + PlayerPrefs.GetInt("HighScore"));
        }
    }

    public void OnEndEdit()
    {
        username = inputField.text;
        Save();
    }
    private void Save()
    {
        StreamWriter writer = new StreamWriter(path);
        writer.WriteLine("Username: "+ username);
        writer.WriteLine("Game Sound: On");
        writer.Close();
        
        Debug.Log("Game saved to \n"+path);
    }

    private void Load()
    {
        StreamReader reader = new StreamReader(path);
        string saveData = reader.ReadToEnd();
        reader.Close();
        Debug.Log("Save game loaded with data: \n" + saveData);
    }
    
}
