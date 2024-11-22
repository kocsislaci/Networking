using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LobbyListUI : MonoBehaviour
{
    private Button _button;

    private int _clickCount;

    //Add logic that interacts with the UI controls in the `OnEnable` methods
    private void OnEnable()
    {
        // The UXML is already instantiated by the UIDocument component
        var uiDocument = GetComponent<UIDocument>();

        _button = uiDocument.rootVisualElement.Q("sign-in") as Button;

        var _inputFields = uiDocument.rootVisualElement.Q("lobby-id");
        _inputFields.RegisterCallback<ChangeEvent<string>>(InputMessage);
    }


    public static void InputMessage(ChangeEvent<string> evt)
    {
        Debug.Log($"{evt.newValue} -> {evt.target}");
    }
}