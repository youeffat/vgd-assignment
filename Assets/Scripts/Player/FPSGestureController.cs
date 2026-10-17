using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PDollarRecognizer;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(SpellDatabase))]
[RequireComponent(typeof(AntiGravitySpell))]
[RequireComponent(typeof(SwapSpell))]
[RequireComponent(typeof(CloneSpell))]
public class FPSGestureController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 9f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;
    private CharacterController controller;
    private Vector3 velocity;

    [Header("Camera Look Settings")]
    public Transform playerCamera;
    public float mouseSensitivity = 2f;
    private float xRotation = 0f;
    private bool canLook = true;

    [Header("UI Settings")]
    public GameObject crosshairUI;

    [Header("Animation Settings")]
    public Animator playerHandsAnimator;

    [Header("Gesture Drawing Library")]
    public List<SymbolTemplate> symbolLibrary;
    public Material lineMaterial;
    public float lineWidth = 0.15f;

    [Header("Symbol Recording Mode")]
    [Tooltip("Check this to record/update a symbol asset instead of casting spells.")]
    public bool isRecordingAssetMode = false;
    [Tooltip("Drag the specific SymbolTemplate asset you want to update here.")]
    public SymbolTemplate symbolTemplateToRecord;

    [Header("Targeting Settings")]
    public float maxSpellRange = 50f;
    public LayerMask targetLayerMask;

    // References & State
    private SpellDatabase spellDatabase;
    private AntiGravitySpell antiGravitySpell;
    private SwapSpell swapSpell;
    private CloneSpell cloneSpell;
    private Recognizer _recognizer;
    private List<Vector2> _allStrokePoints = new List<Vector2>();
    private List<Vector2> _currentStrokePoints = new List<Vector2>();
    private List<LineRenderer> _activeLineRenderers = new List<LineRenderer>();
    private LineRenderer _currentLineRenderer;
    
    private bool isDrawingMode = false;
    private bool _isDrawingStroke = false;
    private Camera cam;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        spellDatabase = GetComponent<SpellDatabase>();
        antiGravitySpell = GetComponent<AntiGravitySpell>();
        swapSpell = GetComponent<SwapSpell>();
        cloneSpell = GetComponent<CloneSpell>();
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main.transform;
        }
        cam = playerCamera.GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        ReloadRecognizerLibrary();
        SetFPSMode();
    }

    public void ReloadRecognizerLibrary()
    {
        _recognizer = new Recognizer();
        foreach (var templateAsset in symbolLibrary)
        {
            if (templateAsset != null && templateAsset.templatePoints.Count > 0)
            {
                _recognizer.SavePattern(templateAsset.symbolIdName, templateAsset.templatePoints);
            }
        }
    }

    void Update()
    {
        if (!isDrawingMode)
        {
            HandleMovement();

            if (canLook)
            {
                HandleFPSLook();
            }

            if (Input.GetMouseButtonDown(1))
            {
                SetDrawingMode();
            }
        }
        else
        {
            HandleDrawingInput();

            // Right click submits normal spell casting
            if (Input.GetMouseButtonDown(1))
            {
                if (!isRecordingAssetMode)
                {
                    ProcessRecognition();
                    TriggerCastAnimation();
                    StartCoroutine(SmoothReturnToFPSRoutine());
                }
            }

            // Press Enter (Return) to record/update the selected symbol asset
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (isRecordingAssetMode && symbolTemplateToRecord != null)
                {
                    UpdateTargetSymbolAsset();
                    TriggerCastAnimation();
                }
                else
                {
                    Debug.LogWarning("Recording mode is off or no Symbol Template asset is assigned!");
                }
                StartCoroutine(SmoothReturnToFPSRoutine());
            }
        }
    }

    void UpdateTargetSymbolAsset()
    {
        if (_allStrokePoints.Count < 5)
        {
            Debug.LogWarning("Drawing is too short to update symbol asset.");
            return;
        }

        // Directly assign Vector2 array to templatePoints
        symbolTemplateToRecord.templatePoints = _allStrokePoints;

    #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(symbolTemplateToRecord);
        UnityEditor.AssetDatabase.SaveAssets();
    #endif

        ReloadRecognizerLibrary();
        Debug.Log($"Successfully updated Symbol Template asset: **{symbolTemplateToRecord.symbolIdName}**!");
    }

    void TriggerCastAnimation()
    {
        if (playerHandsAnimator != null)
        {
            // Set IsDrawing false so it exits drawing state immediately, and fire Cast trigger
            playerHandsAnimator.SetBool("IsDrawing", false);
            playerHandsAnimator.SetTrigger("Cast");
        }
    }

    void HandleMovement()
    {
        // Ground check & gravity reset
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Sprint check (Left Shift)
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Calculate horizontal motion vector
        Vector3 horizontalMove = (transform.right * x + transform.forward * z) * currentSpeed;

        // Jump check (Space)
        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity velocity
        velocity.y += gravity * Time.deltaTime;

        // Combine both into a single Move call so isGrounded registers accurately
        Vector3 finalMove = horizontalMove + new Vector3(0, velocity.y, 0);
        controller.Move(finalMove * Time.deltaTime);
    }

    void HandleFPSLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void SetFPSMode()
    {
        isDrawingMode = false;
        canLook = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (crosshairUI != null) crosshairUI.SetActive(true);
        if (playerHandsAnimator != null) playerHandsAnimator.SetBool("IsDrawing", false);

        ResetDrawing();
    }

    IEnumerator SmoothReturnToFPSRoutine()
    {
        isDrawingMode = false;
        canLook = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (crosshairUI != null) crosshairUI.SetActive(true);
        ResetDrawing();

        if (playerCamera != null)
        {
            xRotation = playerCamera.localEulerAngles.x;
            if (xRotation > 180f) xRotation -= 360f;
        }

        // Cooldown timer set to 0.5s
        float timer = 0f;
        while (timer < 0.5f)
        {
            Input.GetAxis("Mouse X");
            Input.GetAxis("Mouse Y");
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        canLook = true;
    }

    void SetDrawingMode()
    {
        isDrawingMode = true;
        canLook = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (crosshairUI != null) crosshairUI.SetActive(false);
        if (playerHandsAnimator != null) playerHandsAnimator.SetBool("IsDrawing", true);
    }

    void HandleDrawingInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartNewStroke();
        }

        if (_isDrawingStroke && Input.GetMouseButton(0))
        {
            Vector2 mousePos = Input.mousePosition;
            
            if (_currentStrokePoints.Count == 0 || Vector2.Distance(_currentStrokePoints[_currentStrokePoints.Count - 1], mousePos) > 5f)
            {
                _currentStrokePoints.Add(mousePos);
                
                _currentLineRenderer.positionCount = _currentStrokePoints.Count;
                Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));
                _currentLineRenderer.SetPosition(_currentStrokePoints.Count - 1, worldPos);
            }
        }

        if (_isDrawingStroke && Input.GetMouseButtonUp(0))
        {
            _isDrawingStroke = false;

            if (_currentStrokePoints.Count > 5)
            {
                _allStrokePoints.AddRange(_currentStrokePoints);
            }
            else if (_activeLineRenderers.Count > 0)
            {
                LineRenderer emptyLine = _activeLineRenderers[_activeLineRenderers.Count - 1];
                _activeLineRenderers.RemoveAt(_activeLineRenderers.Count - 1);
                Destroy(emptyLine.gameObject);
            }
        }
    }

    void StartNewStroke()
    {
        _isDrawingStroke = true;
        _currentStrokePoints.Clear();

        GameObject strokeObj = new GameObject("StrokeLine");
        strokeObj.transform.SetParent(transform);
        
        _currentLineRenderer = strokeObj.AddComponent<LineRenderer>();
        if (lineMaterial != null) _currentLineRenderer.material = lineMaterial;
        
        _currentLineRenderer.startWidth = lineWidth;
        _currentLineRenderer.endWidth = lineWidth;
        _currentLineRenderer.positionCount = 0;

        _activeLineRenderers.Add(_currentLineRenderer);

        Vector2 mousePos = Input.mousePosition;
        _currentStrokePoints.Add(mousePos);
        _currentLineRenderer.positionCount = 1;
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));
        _currentLineRenderer.SetPosition(0, worldPos);
    }

    void ProcessRecognition()
    {
        if (_allStrokePoints.Count > 10)
        {
            Result result = _recognizer.Recognize(_allStrokePoints);
            Debug.Log($"Recognized Symbol: **{result.Match}** with Score: **{result.Score:F2}**");

            if (result.Score > 0.4f)
            {
                TriggerGameAction(result.Match);
            }
            else
            {
                Debug.Log("Symbol not recognized clearly.");
            }
        }
        else
        {
            Debug.Log("Drawing too short or empty.");
        }
    }

    void ResetDrawing()
    {
        _allStrokePoints.Clear();
        _currentStrokePoints.Clear();
        _isDrawingStroke = false;

        foreach (var lr in _activeLineRenderers)
        {
            if (lr != null) Destroy(lr.gameObject);
        }
        _activeLineRenderers.Clear();
    }

    void TriggerGameAction(string symbolName)
    {
        if (spellDatabase == null) return;

        SpellType? spellType = spellDatabase.GetSpellType(symbolName);

        if (spellType.HasValue)
        {
            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxSpellRange, targetLayerMask))
            {
                Rigidbody rbHit = hit.collider.GetComponent<Rigidbody>();
                if (rbHit != null)
                {
                    if (spellType.Value == SpellType.Clone)
                    {
                        if (hit.collider.CompareTag("Clone") || hit.collider.GetComponent<ClonedObjectTracker>() != null)
                        {
                            Debug.Log("Cannot cast Clone spell on this object; it has already been cloned or is a clone!");
                            return;
                        }
                    }

                    switch (spellType.Value)
                    {
                        case SpellType.AntiGravity:
                            antiGravitySpell.Apply(rbHit);
                            Debug.Log($"Cast Anti-Gravity on {hit.collider.name}!");
                            break;

                        case SpellType.SwapPositions:
                            swapSpell.ExecuteSwap(transform, controller, rbHit);
                            Debug.Log($"Swapped positions with {hit.collider.name}!");
                            break;
                        case SpellType.Clone:
                            cloneSpell.ExecuteClone(hit.collider);
                            Debug.Log($"Cast Clone on {hit.collider.name}!");
                            break;
                    }
                }
                else
                {
                    Debug.Log($"Hit {hit.collider.name}, but it has no Rigidbody.");
                }
            }
            else
            {
                Debug.Log("Spell raycast missed any target.");
            }
        }
        else
        {
            Debug.Log($"No spell assigned for symbol: {symbolName}");
        }
    }
}