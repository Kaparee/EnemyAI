using UnityEngine;
using UnityEngine.InputSystem;
using FMOD.Studio;
using FMODUnity;

public class Move : MonoBehaviour
{
    [SerializeField] public float speed = 10.0f;
    //[SerializeField] private GameObject MapGrid;
    //private bool isPressed = false;

    void Start()
    {
        transform.position = new Vector3(transform.localScale.x, 0.0f, transform.localScale.z);
    }

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
        //if (Keyboard.current.mKey.wasPressedThisFrame)
        //{
        //    isPressed = !isPressed;
        //    MapGrid.SetActive(isPressed);
        //}
    }
}
