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
    public class DialogEditorv1 : EditorWindow
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

        private const string LastDialogPrefKey = "DialogEditorV1_LastDialogGUID";

        // Nodo actualmente seleccionado para mostrar en el panel lateral
        [NonSerialized] DialogNode selectedNode = null;
        // Ancho del panel lateral izquierdo
        const float lateralWindowsL = 250f;
        // Scroll del panel lateral
        Vector2 sidebarScrollPosition;


        // Rect del área del canvas (para compensar el offset del sidebar)
        [NonSerialized] private Rect canvasRect;
        private bool isSelected;

        [MenuItem("Window/Dialogue EditorV1")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(DialogEditorv1), false, "Dialogue EditorV1");

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

            // Restaurar último diálogo abierto
            RestoreLastDialog();
        }

        private void OnSelectionChange()
        {
            Dialog newDialog = Selection.activeObject as Dialog;
            if (newDialog != null)
            {
                selectedDialog = newDialog;
                SaveLastDialog();
                Repaint();
            }
        }

        private void SaveLastDialog()
        {
            if (selectedDialog == null) return;

            string path = AssetDatabase.GetAssetPath(selectedDialog);
            string guid = AssetDatabase.AssetPathToGUID(path);
            EditorPrefs.SetString(LastDialogPrefKey, guid);
        }

        private void RestoreLastDialog()
        {
            if (selectedDialog != null) return;

            string guid = EditorPrefs.GetString(LastDialogPrefKey, "");
            if (string.IsNullOrEmpty(guid)) return;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return;

            Dialog dialog = AssetDatabase.LoadAssetAtPath<Dialog>(path);
            if (dialog != null)
            {
                selectedDialog = dialog;
            }
        }

        private void OnGUI()
        {
            // --- Layout horizontal: [Panel lateral | Canvas] ---
            EditorGUILayout.BeginHorizontal();

            // ========== PANEL LATERAL IZQUIERDO ==========
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(lateralWindowsL));
            LateralWindowsL();
            EditorGUILayout.EndVertical();

            // ========== CANVAS DERECHO (grafo de nodos) ==========
            EditorGUILayout.BeginVertical();

            // Capturar el rect del panel derecho en coordenadas de ventana
            // usando un truco: dibujamos un rect invisible de 0px y leemos su posición
            Rect marker = GUILayoutUtility.GetRect(0, 0);
            if (Event.current.type == EventType.Repaint)
            {
                canvasRect = marker;
            }

            LateralWindowsR();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void LateralWindowsL()
        {
            // --- Título ---
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Parameters", titleStyle);

            // Línea separadora
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Si no hay diálogo seleccionado, mostrar solo el título
            if (selectedDialog == null)
            {
                EditorGUILayout.LabelField("No Dialogue Selected", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            // Mostrar referencia al diálogo activo (solo lectura)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Dialogue", selectedDialog, typeof(Dialog), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8);

            // Si no hay nodo seleccionado, indicarlo
            if (selectedNode == null)
            {
                EditorGUILayout.LabelField("Click a node to inspect", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            // --- Parámetros del nodo seleccionado ---
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Scroll por si hay muchos parámetros
            sidebarScrollPosition = EditorGUILayout.BeginScrollView(sidebarScrollPosition);

            // ID (solo lectura)
            EditorGUILayout.LabelField("Node ID", selectedNode.name, EditorStyles.boldLabel);

            EditorGUILayout.Space(4);

            // --- Speaker ---
            EditorGUILayout.LabelField("Speaker", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            bool newSpeaker = EditorGUILayout.Toggle("Is Player Speaking", selectedNode.IsPlayerSpeaking());
            if (EditorGUI.EndChangeCheck())
            {
                selectedNode.SetPlayerSpeaking(newSpeaker);
                Repaint();
            }

            EditorGUILayout.Space(4);

            // --- Texto del diálogo ---
            EditorGUILayout.LabelField("Dialogue Text", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            string newText = EditorGUILayout.TextArea(selectedNode.GetText() ?? "", GUILayout.MinHeight(60));
            if (EditorGUI.EndChangeCheck())
            {
                selectedNode.SetText(newText);
            }

            EditorGUILayout.Space(4);

            // --- Costes ---
            EditorGUILayout.LabelField("Costs", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            float newMoney = EditorGUILayout.FloatField("Money Cost", selectedNode.GetMoneyCost());
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(selectedNode, "Edit Money Cost");
                selectedNode.SetMoneyCost(newMoney);
                EditorUtility.SetDirty(selectedNode);
            }

            EditorGUI.BeginChangeCheck();
            int newBombs = EditorGUILayout.IntField("Bombs Cost", selectedNode.GetBombsCost());
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(selectedNode, "Edit Bombs Cost");
                selectedNode.SetBombsCost(newBombs);
                EditorUtility.SetDirty(selectedNode);
            }

            EditorGUI.BeginChangeCheck();
            bool newBombsFlag = EditorGUILayout.Toggle("Bombs False Se", selectedNode.GetBombsFalseSe());
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(selectedNode, "Edit Bombs Flag");
                selectedNode.SetBombsFalseSe(newBombsFlag);
                EditorUtility.SetDirty(selectedNode);
            }

            EditorGUILayout.Space(4);

            // --- Children (solo lectura informativa) ---
            EditorGUILayout.LabelField("Children", EditorStyles.boldLabel);
            var children = selectedNode.GetChildren();
            if (children.Count == 0)
            {
                EditorGUILayout.LabelField("  (none)", EditorStyles.miniLabel);
            }
            else
            {
                foreach (string childID in children)
                {
                    EditorGUILayout.LabelField("  → Node " + childID, EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndScrollView();
            if (EditorGUI.EndChangeCheck())
            {
                Repaint();
            }
        }
        private void LateralWindowsR()
        {
            // Si no hay ningún diálogo seleccionado, muestra un mensaje informativo y sale
            if (selectedDialog == null)
            {

                EditorGUILayout.LabelField("No Dialogue Selected", EditorStyles.boldLabel);
                return;
            }
            else
            {
                // Procesa eventos de input (arrastre de nodos, zoom, clic en canvas, etc.)
                ProcessEvents();

                // Inicia un ScrollView para permitir desplazamiento por el canvas
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                // Reserva un espacio rectangular del tamaño del canvas para dibujar sobre él
                Rect canvas = GUILayoutUtility.GetRect(canvasSize, canvasSize);

                // Carga la textura de fondo y calcula las coordenadas UV para repetirla en mosaico
                Texture2D backgroundTex = Resources.Load("background") as Texture2D;
                Rect texCoords = new(0, 0, canvasSize / backgroundSize, canvasSize / backgroundSize);
                // Dibuja la textura de fondo repetida sobre toda el área del canvas
                GUI.DrawTextureWithTexCoords(canvas, backgroundTex, texCoords);

                // Primer pase: dibuja las líneas bezier de conexión entre nodos
                // (se dibujan primero para que queden detrás de los nodos)
                foreach (DialogNode node in selectedDialog.GetAllNodes())
                {
                    DrawConnections(node);
                }

                // Segundo pase: dibuja los nodos encima de las conexiones
                foreach (DialogNode node in selectedDialog.GetAllNodes())
                {
                    DrawNode(node);
                }

                // Cierra el ScrollView
                EditorGUILayout.EndScrollView();

                // --- Acciones diferidas ---
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
                    {
                        if (deletingNode == selectedNode)
                            selectedNode = null;
                        selectedDialog.DeleteNode(deletingNode);
                    }
                    deletingNode = null;
                }

            }
        }
        private void ProcessEvents()
        {
            // Obtener el evento current en la ventana de edición
            Event evento = Event.current;

            // Compensar solo el offset horizontal del sidebar.
            // El eje Y no necesita compensación porque el ScrollView ya lo gestiona
            // con scrollPosition internamente.
            Vector2 canvasMousePos = evento.mousePosition;
            canvasMousePos.x -= canvasRect.x;

            // comprueba si el evento es una rueda de mouse y si se esta presionando Ctrl
            if (evento.type == EventType.ScrollWheel && (evento.control || evento.command))
            {
                // Calcula la posición del ratón relativa al contenido del canvas (teniendo en cuenta el scroll y el zoom).
                Vector2 contentPos = (canvasMousePos + scrollPosition) / zoom;
                // Determina la direccion del scroll y calcula el nuevo zoom
                float delta = -evento.delta.y; // rueda arriba positivo
                float target = Mathf.Clamp(zoom + delta * ZoomStep * 0.2f, MinZoom, MaxZoom);

                // Si el zoom ha cambiado, ajusta el scroll para que el punto bajo el ratón permanezca fijo en la pantalla.
                if (!Mathf.Approximately(target, zoom))
                {
                    scrollPosition = contentPos * target - canvasMousePos;
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
                    draggingNode = GetNodeAtPoint((canvasMousePos + scrollPosition) / zoom);
                    isSelected = draggingNode != null;
                    if (draggingNode != null)
                    {
                        draggingOffsets = draggingNode.GetRect().position - ((canvasMousePos + scrollPosition) / zoom);
                        Selection.activeObject = draggingNode;
                        // Guardar referencia al nodo seleccionado para el panel lateral
                        selectedNode = draggingNode;
                    }
                    else
                    {
                        // Clic izquierdo en vacío: deselecciona el nodo y selecciona el diálogo
                        selectedNode = null;
                        Selection.activeObject = selectedDialog;
                        isSelected = false;
                    }
                }
                // Botón medio: arrastrar el canvas
                else if (evento.button == 2)
                {
                    isDraggingCanvas = true;
                    draggingCanvasOffset = canvasMousePos + scrollPosition;
                }
            }
            // ----- Arrastre en curso -----
            // Si se esta arrastrando el mouse y hay un nodo seleccionado para arrrastrar.
            else if (evento.type == EventType.MouseDrag && draggingNode != null)
            {
                // actualiza la posicion del nodo basandose en la posición del ratón y el desfase inicial.
                draggingNode.SetPosition(((canvasMousePos + scrollPosition) / zoom) + draggingOffsets);
                GUI.changed = true; // Marca la GUI para redibujar.
            }
            // Si se está arrastrando el ratón y es el canvas el que se mueve.
            else if (evento.type == EventType.MouseDrag && isDraggingCanvas)
            {
                // Actualiza la posición del scroll para mover el canvas.
                scrollPosition = draggingCanvasOffset - canvasMousePos;
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
            // Selecciona el estilo visual según si habla el jugador o el NPC
            GUIStyle style = node.IsPlayerSpeaking() ? PlayerNodeStyle : nodeStyle;

            // Obtiene el rectángulo original del nodo y lo escala según el zoom actual
            Rect r = node.GetRect();
            Rect drawRect = new(r.x * zoom, r.y * zoom, r.width * zoom, r.height * zoom);

            // Inicia un área de dibujo IMGUI en la posición y tamaño escalados con el estilo del nodo
            GUILayout.BeginArea(drawRect, style);

            // Muestra el identificador del nodo en negrita
            EditorGUILayout.LabelField("ID: " + node.name, EditorStyles.whiteBoldLabel);
            // Etiqueta descriptiva para la sección de texto
            EditorGUILayout.LabelField("Dialogue:");
            //node.SetText(EditorGUILayout.TextArea(node.GetText()));


            // Altura máxima visible del área de texto antes de activar scroll interno
            const float maxTextHeight = 60f; // Cambia si quieres más alto (ej. 200f)
            GUIStyle textAreaStyle = GUI.skin.textArea;
            string currentText = node.GetText();

            // Calcula el ancho disponible para el texto descontando padding del nodo
            float contentWidth = drawRect.width - 40f;
            if (contentWidth < 50f) contentWidth = drawRect.width;

            // Calcula la altura real que necesita el texto completo para mostrarse
            float fullHeight = textAreaStyle.CalcHeight(new GUIContent(currentText), contentWidth);
            // Determina si el texto desborda la altura máxima permitida
            bool overflow = fullHeight > maxTextHeight;
            // Altura que se mostrará: la máxima si hay overflow, o la real si cabe
            float shownHeight = overflow ? maxTextHeight : fullHeight;

            // Recupera la posición de scroll guardada para este nodo (si existe)
            Vector2 scroll;
            if (!nodeTextScrollPositions.TryGetValue(node.name, out scroll))
                scroll = Vector2.zero;

            // Inicia la detección de cambios en los controles del editor
            EditorGUI.BeginChangeCheck();
            if (overflow)
            {
                // Si el texto desborda, lo envuelve en un ScrollView con altura limitada
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(maxTextHeight));

                // Muestra el TextArea con la altura completa del texto (scrollable)
                string newText = EditorGUILayout.TextArea(currentText, textAreaStyle, GUILayout.Height(fullHeight));
                EditorGUILayout.EndScrollView();

                // Si el texto cambió, registra el cambio para soporte de Undo
                if (EditorGUI.EndChangeCheck() && newText != currentText)
                {
                    Undo.RecordObject(node, "Edit Dialogue Text");
                    node.SetText(newText);
                    EditorUtility.SetDirty(node);
                }
            }
            else
            {
                // Si el texto cabe, muestra el TextArea sin scroll
                string newText = EditorGUILayout.TextArea(currentText, textAreaStyle, GUILayout.Height(shownHeight));
                if (EditorGUI.EndChangeCheck() && newText != currentText)
                {
                    Undo.RecordObject(node, "Edit Dialogue Text");
                    node.SetText(newText);
                    EditorUtility.SetDirty(node);
                }
            }

            // Guarda la posición de scroll actual para este nodo
            nodeTextScrollPositions[node.name] = scroll;



            // --- Barra horizontal de botones de acción ---
            GUILayout.BeginHorizontal();


            // Botón para eliminar el nodo
            if (LeftClickButton("x") && !isSelected)
            {
                deletingNode = node;
            }
            // Botones de enlace (link/unlink/child/cancel)
            DrawLinkButtons(node);

            // Botón para crear un nodo hijo
            if (LeftClickButton("+") && !isSelected)
            {
                creatingNode = node;
            }
            GUILayout.EndHorizontal();

            // Toggle para alternar entre hablante Player/NPC
            DrawStatePlayer(node);

            // Cierra el área de dibujo del nodo
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
                if (LeftClickButton("link") && !isSelected)
                {
                    linkingParentNode = node;
                }
            }
            else if (linkingParentNode == node)
            {
                if (LeftClickButton("cancel") && !isSelected)
                {
                    linkingParentNode = null;
                }
            }
            else if (linkingParentNode.GetChildren().Contains(node.name))
            {
                if (LeftClickButton("Unlink") && !isSelected)
                {

                    linkingParentNode.RemoveChild(node.name);
                    linkingParentNode = null;
                }
            }
            else
            {
                if (LeftClickButton("Child") && !isSelected)
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
