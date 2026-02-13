using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEditor;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Dialogs
{
    //[CreateAssetMenu(fileName = "New DialogNode", menuName = "Scriptable Objects/Dialogue/DialogNode", order = 1)]
    public class DialogNode : ScriptableObject
    {

        [SerializeField,IReadOnly] private string uniqueID;
        [SerializeField] private bool isPlayerSpeaking = false;
        [SerializeField] private string text;
        [SerializeField] private List<string> childrenIDs = new();
        [SerializeField, HideInInspector] private Rect rect = new(0, 0, 300, 200);
        [SerializeField] private float MoneyCost = 0f;
        [SerializeField] private int BombsCost = 0;
        [SerializeField] private bool Bombsfalsese = false;

        public Rect GetRect()
        {
            return rect;
        }
        public string GetText()
        {
            return text;
        }
        public List<string> GetChildren()
        {
            return childrenIDs;
        }
        public bool IsPlayerSpeaking()
        {
            return isPlayerSpeaking;
        }
        public string GetID()
        {
            return uniqueID;
        }
        public float GetMoneyCost()
        {
            return MoneyCost;
        }
        public int GetBombsCost()
        {
            return BombsCost;
        }
        public bool GetBombsFalseSe()
        {
            return Bombsfalsese;
        }
        public void SetMoneyCost(float cost)
        {
            MoneyCost = cost;
        }
        public void SetBombsCost(int cost)
        {
            BombsCost = cost;
        }
        public void SetBombsFalseSe(bool state)
        {
            Bombsfalsese = state;
        }
#if UNITY_EDITOR
        public void SetPosition(Vector2 newPosition)
        {
            Undo.RecordObject(this, "Drag Dialog Node");
            rect.position = newPosition;
            EditorUtility.SetDirty(this);
        }
        public void SetText(string newText)
        {
            if (newText != text)
            {
                Undo.RecordObject(this, "Update Dialog Text");
                text = newText;
                EditorUtility.SetDirty(this);
            }
        }
        public void AddChild(string childID)
        {
            Undo.RecordObject(this, "Add Child To Dialog Node");
            childrenIDs.Add(childID);
            EditorUtility.SetDirty(this);
        }
        public void RemoveChild(string childID)
        {
            Undo.RecordObject(this, "Remove Child From Dialog Node");
            childrenIDs.Remove(childID);
            EditorUtility.SetDirty(this);
        }

        public void SetPlayerSpeaking(bool newIsplayerSpeking)
        {
            Undo.RecordObject(this, "Set Player Speaking");
            isPlayerSpeaking = newIsplayerSpeking;
            EditorUtility.SetDirty(this);
        }
        public void SetID(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            if (uniqueID != id || name != id)
            {
                uniqueID = id;
                name = id;
                UnityEditor.EditorUtility.SetDirty(this);

            }
        }

#endif
    }
}
