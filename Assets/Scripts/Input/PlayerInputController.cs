using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private TilemapBoardAdapter board;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private ActionMenuUI actionMenuUI;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Vector3 projectileSpawnOffset = new Vector3(0f, 0.80f, 0f);
    [SerializeField] private Vector3 projectileHitOffset = new Vector3(0f, 0.15f, 0f);

    [Header("Sword Slash")]
    [SerializeField] private GameObject swordSlashPrefab;

    [Header("Selection")]
    [SerializeField] private LayerMask unitLayerMask;

    [Header("Unit Cards")]
    [SerializeField] private UnitCardUI playerCard;
    [SerializeField] private UnitCardUI enemyCard;

    // Former field name remains readable by Unity; live selection belongs to commandState.
    [UnityEngine.Serialization.FormerlySerializedAs("selectedUnit")]
    [SerializeField] private CombatUnit initialSelectedUnit;

    private PlayerCommandState commandState;
    private PlayerMovementExecutor movement;
    private PlayerAttackExecutor attacks;
    private PlayerBattleUI battleUI;

    public CombatUnit selectedUnit
    {
        get => commandState != null ? commandState.SelectedUnit : initialSelectedUnit;
        set
        {
            if (commandState == null) initialSelectedUnit = value;
            else if (value == null) commandState.ClearSelection();
            else commandState.SelectUnit(value);
        }
    }

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        battleUI = new PlayerBattleUI(board, actionMenuUI, playerCard, enemyCard);
        commandState = new PlayerCommandState(turnManager, board, battleUI);
        movement = new PlayerMovementExecutor(this, turnManager, board, commandState, battleUI);
        attacks = new PlayerAttackExecutor(this, turnManager, board, commandState, battleUI,
            projectilePrefab, projectileSpawnOffset, projectileHitOffset, swordSlashPrefab);
        if (initialSelectedUnit != null) commandState.RestoreSelectedUnit(initialSelectedUnit);
        initialSelectedUnit = null;
        battleUI.HideMenu();
    }

    private void Update()
    {
        if (turnManager == null || board == null || mainCamera == null || turnManager.IsBattleOver)
            return;

        if (!turnManager.IsPlayerTurn())
            return;

        if (commandState.IsResolving)
            return;

        commandState.ValidateSelection();
        HandleCommandHotkeys();
        battleUI.PreviewHover(commandState, GetMouseWorldPosition());

        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }

        if (Input.GetMouseButtonDown(1))
        {
            commandState.CancelCurrentMode();
        }

    }

    private void HandleLeftClick()
    {
        Vector3 worldPos = GetMouseWorldPosition();

        if (commandState.Mode == CommandMode.Move && selectedUnit != null)
        {
            GridCoord clickedCoord = board.ConvertWorldToGrid(worldPos);

            if (commandState.ReachableCells.Contains(clickedCoord) && !(clickedCoord.x == selectedUnit.GridPosition.x && clickedCoord.y == selectedUnit.GridPosition.y))
            {
                movement.CommitMove(clickedCoord);
                return;
            }
            else
            {
                SFXManager.InvalidAction();
            }
        }

        if (PlayerCommandState.IsAttackMode(commandState.Mode) && selectedUnit != null)
        {
            GridCoord clickedCoord = board.ConvertWorldToGrid(worldPos);
            attacks.TryCommitAttackAt(clickedCoord);
            return; 
        }

        Collider2D hit = Physics2D.OverlapPoint(worldPos, unitLayerMask);
        if (hit == null)
            return;

        CombatUnit clickedUnit = hit.GetComponent<CombatUnit>();
        if (clickedUnit == null)
            return;

        if (!clickedUnit.IsAlive)
            return;

        SFXManager.UnitSelect();

        commandState.SelectUnit(clickedUnit);
    }

    private void HandleCommandHotkeys()
    {
        if (selectedUnit == null)
            return;

        if (Input.GetKeyDown(KeyCode.M))
        {
            SelectMoveCommand();
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            SelectMeleeAttackCommand();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SelectRangedAttackCommand();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            EndSelectedUnitTurn();
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = -mainCamera.transform.position.z;
        Vector3 world = mainCamera.ScreenToWorldPoint(mouse);
        world.z = 0f;
        return world;
    }

    public void SelectMoveCommand() => commandState?.RequestCommand(CommandMode.Move);
    public void SelectRangedAttackCommand() => commandState?.RequestCommand(CommandMode.RangedAttack);
    public void SelectMeleeAttackCommand() => commandState?.RequestCommand(CommandMode.MeleeAttack);
    public void EndSelectedUnitTurn() => commandState?.EndPlayerTurn();

    // Existing UnitVisualAnimator animation-event bridge.
    public void AnimEvent_FireRangedProjectile(CombatUnit eventOwner)
        => attacks?.AnimEvent_FireRangedProjectile(eventOwner);
    public void AnimEvent_RangedAttackFinished(CombatUnit eventOwner)
        => attacks?.AnimEvent_RangedAttackFinished(eventOwner);
}
