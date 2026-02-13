using Codice.CM.Common.Mount;
using PlasticGui.WorkspaceWindow;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dialogs.Editor
{
    public class DialogEditor : EditorWindow
    {
        Dialog selectedDialog = null;
        [NonSerialized] GUIStyle nodeStyle;
        [NonSerialized] GUIStyle PlayerNodeStyle;
        [NonSerialized] DialogNode draggingNode = null;
        [NonSerialized] Vector2 draggingOffsets;
        [NonSerialized] DialogNode creatingNode = null;
        [NonSerialized] DialogNode deletingNode = null;
        [NonSerialized] DialogNode linkingParentNode = null;
        Vector2 scrollPosition;
        [NonSerialized] bool isDraggingCanvas = false;
        [NonSerialized] Vector2 draggingCanvasOffset;
        const float canvasSize = 4000f;
        const float backgroundSize = 50f;

        [NonSerialized] Dictionary<string, Vector2> nodeTextScrollPositions = new();
        [NonSerialized] float zoom = 1f;
        const float MinZoom = 0.4f;
        const float MaxZoom = 2.5f;
        const float ZoomStep = 0.1f;


        [MenuItem("Window/Dialogue EditorV1")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(DialogEditor), false, "Dialogue EditorV1");

        }

        [OnOpenAssetAttribute(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            Dialog dialog = EditorUtility.InstanceIDToObject(instanceID) as Dialog;

            if (dialog != null)
            {
                ShowEditorWindow();
                return true;
            }
            return false;
        }
        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChange;

            nodeStyle = new GUIStyle();
            nodeStyle.normal.background = EditorGUIUtility.Load("node0") as Texture2D;
            nodeStyle.normal.textColor = Color.white;
            nodeStyle.padding = new RectOffset(20, 20, 20, 20);
            nodeStyle.border = new RectOffset(12, 12, 12, 12);

            PlayerNodeStyle = new GUIStyle();
            PlayerNodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
            PlayerNodeStyle.normal.textColor = Color.blue;
            PlayerNodeStyle.padding = new RectOffset(20, 20, 20, 20);
            PlayerNodeStyle.border = new RectOffset(12, 12, 12, 12);

        }

        private void OnSelectionChange()
        {
            Dialog newDialog = Selection.activeObject as Dialog;
            if (newDialog != null)
            {
                selectedDialog = newDialog;
                Repaint();
            }
        }

        private void OnGUI()
        {
            if (selectedDialog == null)
            {

                EditorGUILayout.LabelField("No Dialogue Selected", EditorStyles.boldLabel);
                return;
            }
            else
            {
                ProcessEvents();
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                Rect canvas = GUILayoutUtility.GetRect(canvasSize, canvasSize);
                Texture2D backgroundTex = Resources.Load("background") as Texture2D;
                Rect texCoords = new(0, 0, canvasSize / backgroundSize, canvasSize / backgroundSize);
                GUI.DrawTextureWithTexCoords(canvas, backgroundTex, texCoords);

                foreach (DialogNode node in selectedDialog.GetAllNodes())
                {
                    DrawConnections(node);
                }
                foreach (DialogNode node in selectedDialog.GetAllNodes())
                {
                    DrawNode(node);
                }

                EditorGUILayout.EndScrollView();

                if (creatingNode != null)
                {
                    selectedDialog.CreateNode(creatingNode);
                    creatingNode = null;
                }
                if (deletingNode != null)
                {
                    if (selectedDialog.GetAllNodesCount() <= 1)
                        EditorUtility.DisplayDialog("Delete Node", "Cannot delete the last node in the dialogue.", "OK");
                    else
                        selectedDialog.DeleteNode(deletingNode);
                    deletingNode = null;
                }

            }
        }
        private void ProcessEvents()
        {
            // Obtener el evento actual en la ventana de edición
            Event evento = Event.current;
            

            // comprueba si el evento es una rueda de mouse y si se esta presionando Ctrl
            if (evento.type == EventType.ScrollWheel && (evento.control || evento.command))
            {
                // Calcula la posición del ratón relativa al contenido del canvas (teniendo en cuenta el scroll y el zoom).
                Vector2 mousePos = evento.mousePosition;
                Vector2 contentPos = (mousePos + scrollPosition) / zoom;
                // Determina la direccion del scroll y calcula el nuevo zoom
                float delta = -evento.delta.y; // rueda arriba positivo
                float target = Mathf.Clamp(zoom + delta * ZoomStep * 0.2f, MinZoom, MaxZoom);

                // Si el zoom ha cambiado, ajusta el scroll para que el punto bajo el ratón permanezca fijo en la pantalla.
                if (!Mathf.Approximately(target, zoom))
                {
                    scrollPosition = contentPos * target - mousePos;
                    zoom = target;
                    GUI.changed = true;// Indica que la GUI ha cambiado para que se repinte con el nuevo zoom y scroll.
                    evento.Use(); //consume el evento para evitar que otros elementos lo procesen (como el scroll normal de la ventana).
                }
            }

            // ----- Manejo de arrastrar nodos y canvas -----
            //comprueba si el evento es un clic del mouse y no se esta arrastrando ningún nodo actualmente
            if (evento.type == EventType.MouseDown && draggingNode == null && !isDraggingCanvas)
            {
                // Botón izquierdo: arrastrar nodos
                if (evento.button == 0)
                {
                    draggingNode = GetNodeAtPoint((evento.mousePosition + scrollPosition) / zoom);
                    if (draggingNode != null)
                    {
                        draggingOffsets = draggingNode.GetRect().position - ((evento.mousePosition + scrollPosition) / zoom);
                        Selection.activeObject = draggingNode;
                    }
                    else
                    {
                        // Clic izquierdo en vacío: selecciona el diálogo
                        Selection.activeObject = selectedDialog;
                    }
                }
                // Botón medio: arrastrar el canvas
                else if (evento.button == 2)
                {
                    isDraggingCanvas = true;
                    draggingCanvasOffset = evento.mousePosition + scrollPosition;
                }
            }
            // ----- Arrastre en curso -----
            // Si se esta arrastrando el mouse y hay un nodo seleccionado para arrrastrar.
            else if (evento.type == EventType.MouseDrag && draggingNode != null)
            {
                // actualiza la posicion del nodo basandose en la posición del ratón y el desfase inicial.
                draggingNode.SetPosition(((evento.mousePosition + scrollPosition) / zoom) + draggingOffsets);
                GUI.changed = true; // Marca la GUI para redibujar.
            }
            // Si se está arrastrando el ratón y es el canvas el que se mueve.
            else if (evento.type == EventType.MouseDrag && isDraggingCanvas)
            {
                // Actualiza la posición del scroll para mover el canvas.
                scrollPosition = draggingCanvasOffset - evento.mousePosition;
                GUI.changed = true;
            }
            // --- FIN DE ARRASTRE ---
            // Si se suelta el botón del ratón y se estaba arrastrando un nodo.
            else if (evento.type == EventType.MouseUp && draggingNode != null)
            {
                draggingNode = null;// Finaliza el arrastre del nodo.
            }
            // Si se suelta el botón del ratón y se estaba arrastrando el canvas.
            else if (evento.type == EventType.MouseUp && isDraggingCanvas)
            {
                isDraggingCanvas = false;// Finaliza el arrastre del canvas.
            }
        }



        private void DrawNode(DialogNode node)
        {
            GUIStyle style = node.IsPlayerSpeaking() ? PlayerNodeStyle : nodeStyle;
            Rect r = node.GetRect();
            Rect drawRect = new(r.x * zoom, r.y * zoom, r.width * zoom, r.height * zoom);
            GUILayout.BeginArea(drawRect, style);



            //GUIStyle style = nodeStyle;
            //if (node.IsPlayerSpeaking())
            //{
            //    style = PlayerNodeStyle;
            //}
            //GUILayout.BeginArea(node.GetRect(), style);

            EditorGUILayout.LabelField("ID: " + node.name, EditorStyles.whiteBoldLabel);
            EditorGUILayout.LabelField("Dialogue:");
            //node.SetText(EditorGUILayout.TextArea(node.GetText()));


            const float maxTextHeight = 60f; // Cambia si quieres más alto (ej. 200f)
            GUIStyle textAreaStyle = GUI.skin.textArea;
            string currentText = node.GetText();


            float contentWidth = drawRect.width - 40f;
            if (contentWidth < 50f) contentWidth = drawRect.width;

            float fullHeight = textAreaStyle.CalcHeight(new GUIContent(currentText), contentWidth);
            bool overflow = fullHeight > maxTextHeight;
            float shownHeight = overflow ? maxTextHeight : fullHeight;


            Vector2 scroll;
            if (!nodeTextScrollPositions.TryGetValue(node.name, out scroll))
                scroll = Vector2.zero;

            EditorGUI.BeginChangeCheck();
            if (overflow)
            {

                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(maxTextHeight));

                string newText = EditorGUILayout.TextArea(currentText, textAreaStyle, GUILayout.Height(fullHeight));
                EditorGUILayout.EndScrollView();

                if (EditorGUI.EndChangeCheck() && newText != currentText)
                {
                    Undo.RecordObject(node, "Edit Dialogue Text");
                    node.SetText(newText);
                    EditorUtility.SetDirty(node);
                }
            }
            else
            {

                string newText = EditorGUILayout.TextArea(currentText, textAreaStyle, GUILayout.Height(shownHeight));
                if (EditorGUI.EndChangeCheck() && newText != currentText)
                {
                    Undo.RecordObject(node, "Edit Dialogue Text");
                    node.SetText(newText);
                    EditorUtility.SetDirty(node);
                }
            }


            nodeTextScrollPositions[node.name] = scroll;



            GUILayout.BeginHorizontal();

            if (LeftClickButton("x"))
            {
                deletingNode = node;
            }
            DrawLinkButtons(node);

            if (LeftClickButton("+"))
            {
                creatingNode = node;
            }
            GUILayout.EndHorizontal();
            DrawStatePlayer(node);

            GUILayout.EndArea();
        }

        private bool LeftClickButton(string text)
        {
            Event evt = Event.current;
            // Reserva el espacio del botón y obtiene su Rect.
            Rect rect = GUILayoutUtility.GetRect(new GUIContent(text), GUI.skin.button);
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            switch (evt.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    // Solo acepta clic izquierdo (button == 0) dentro del área del botón.
                    if (rect.Contains(evt.mousePosition) && evt.button == 0)
                    {
                        GUIUtility.hotControl = controlId;
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                        // Solo devuelve true si se soltó dentro del botón.
                        return rect.Contains(evt.mousePosition);
                    }
                    break;

                case EventType.Repaint:
                    GUI.skin.button.Draw(rect, new GUIContent(text), controlId, false, rect.Contains(evt.mousePosition));
                    break;
            }

            return false;
        }

        private static void DrawStatePlayer(DialogNode node)
        {
            bool current = node.IsPlayerSpeaking();
            // Mostrar un toggle donde true = Player hablando, false = NPC hablando.
            string label = "Player Speaking";
            bool next = GUILayout.Toggle(current, label);

            if (next != current)
            {
                Undo.RecordObject(node, "Toggle Speaker");
                node.SetPlayerSpeaking(next);
                EditorUtility.SetDirty(node);
            }
        }

        private void DrawLinkButtons(DialogNode node)
        {
            if (linkingParentNode == null)
            {
                if (LeftClickButton("link"))
                {
                    linkingParentNode = node;
                }
            }
            else if (linkingParentNode == node)
            {
                if (LeftClickButton("cancel"))
                {
                    linkingParentNode = null;
                }
            }
            else if (linkingParentNode.GetChildren().Contains(node.name))
            {
                if (LeftClickButton("Unlink"))
                {

                    linkingParentNode.RemoveChild(node.name);
                    linkingParentNode = null;
                }
            }
            else
            {
                if (LeftClickButton("Child"))
                {
                    Undo.RecordObject(selectedDialog, "Link Dialog Nodes");
                    linkingParentNode.AddChild(node.name);
                    linkingParentNode = null;
                }
            }
        }

        private void DrawConnections(DialogNode node)
        {
            Rect r = node.GetRect();
            Vector2 start = new(r.xMax * zoom - 7 * zoom, r.center.y * zoom);
            foreach (var child in selectedDialog.GetAllChildren(node))
            {
                Rect cr = child.GetRect();
                Vector2 end = new(cr.xMin * zoom + 7 * zoom, cr.center.y * zoom);
                Vector2 cp = end - start;
                cp.y = 0; cp.x *= 0.8f;
                Handles.DrawBezier(start, end, start + cp, end - cp, Color.white, null, 4f);
            }
        }

        private DialogNode GetNodeAtPoint(Vector2 mousePoint)
        {
            DialogNode found = null;
            foreach (var n in selectedDialog.GetAllNodes())
                if (n.GetRect().Contains(mousePoint))
                    found = n;
            return found;
        }
    }
}
