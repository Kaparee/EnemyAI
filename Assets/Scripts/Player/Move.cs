using UnityEngine;
using UnityEngine.InputSystem;

// Moduł sterujący prostym, liniowym przemieszczaniem, wykorzystywany jako podstawa ruchu dla bazowych jednostek.
public class Move : MonoBehaviour
{
    [SerializeField] public float speed = 10.0f;

    // Ustawia biezaca pozycje na wektor inicjalizacyjny, skalujac go na podstawie wymiarow z zachowaniem plaskiej osi Y.
    void Start()
    {
        transform.position = new Vector3(transform.localScale.x, 0.0f, transform.localScale.z);
    }

    // Główna pętla logiczna klatki. Staram się tu minimalizować ciężkie obliczenia.

    // Translokuje pozycje obiektu z wykorzystaniem systemowego zegara biorac pod uwage odczyty klawiszy wejscia.
    void Update()
    {
        if (GameManager.Instance.currentState != GameState.Exploration) return;
        
        if (Keyboard.current.wKey.isPressed)
        {
            transform.Translate(Vector3.forward * Time.deltaTime * speed);
        }
        if (Keyboard.current.sKey.isPressed)
        {
            transform.Translate(Vector3.back * Time.deltaTime * speed);
        }
        if (Keyboard.current.dKey.isPressed)
        {
            transform.Translate(Vector3.right * Time.deltaTime * speed);
        }
        if (Keyboard.current.aKey.isPressed)
        {
            transform.Translate(Vector3.left * Time.deltaTime * speed);
        }
        if (Keyboard.current.spaceKey.isPressed) 
        {
            transform.Translate(Vector3.up * Time.deltaTime * speed);
        }
        if (Keyboard.current.shiftKey.isPressed)
        {
            transform.Translate(Vector3.down * Time.deltaTime * speed);
        }
    }
}
