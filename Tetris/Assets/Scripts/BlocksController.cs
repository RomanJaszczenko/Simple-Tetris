using UnityEditor.VersionControl;
using UnityEngine;

public class BlocksController : MonoBehaviour
{
    public GameGrid Grid { get; private set; }
    public TetrominoData Data { get; private set; }
    public Vector3Int[] Cells { get; private set; }
    public Vector3Int Position { get; private set; }
    public int RotationIndex { get; private set; }

    public float stepDelay = 1f;
    public float moveDelay = 0.1f;
    public float lockDelay = 0.5f;

    private float stepTime;
    private float moveTime;
    private float lockTime;

    public void Initialize(GameGrid board, Vector3Int position, TetrominoData data)
    {
        Data = data;
        Grid = board;
        Position = position;

        RotationIndex = 0;
        stepTime = Time.time + stepDelay;
        moveTime = Time.time + moveDelay;
        lockTime = 0f;

        if (Cells == null)
        {
            Cells = new Vector3Int[data.cells.Length];
        }

        for (int i = 0; i < Cells.Length; i++)
        {
            Cells[i] = (Vector3Int)data.cells[i];
        }
    }

    private void Update()
    {
        HandleKeyboardInputs();
    }

    private void HandleKeyboardInputs()
    {
        Grid.Clear(this);

        //Timer before it locks in place
        lockTime += Time.deltaTime;

        // Handle rotation
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Rotate(-1);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            Rotate(1);
        }

        // Process instant drop
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HardDrop();
        }

        // Enable player to press movement keys, but introduce a delay to prevent excessively fast movement
        if (Time.time > moveTime)
        {
            HandleMoveInputs();
        }

        // Progress the piece to the next row at regular intervals
        if (Time.time > stepTime)
        {
            Step();
        }

        Grid.Set(this);
    }

    private void HandleMoveInputs()
    {
        // Soft drop movement
        if (Input.GetKey(KeyCode.S))
        {
            if (Move(Vector2Int.down))
            {
                stepTime = Time.time + stepDelay;
            }
        }

        // Horizontal movement (left/right)
        if (Input.GetKey(KeyCode.A))
        {
            Move(Vector2Int.left);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            Move(Vector2Int.right);
        }
    }

    private void Step()
    {
        stepTime = Time.time + stepDelay;

        // Step down to the next row
        Move(Vector2Int.down);

        // Once the block has been inactive for too long it becomes locked
        if (lockTime >= lockDelay)
        {
            Lock();
        }
    }

    private void HardDrop()
    {
        while (Move(Vector2Int.down))
        {
            continue;
        }

        Lock();
    }

    private void Lock()
    {
        Grid.Set(this);
        Grid.ClearLines();
        Grid.SpawnPiece();
    }

    private bool Move(Vector2Int translation)
    {
        Vector3Int newPosition = Position;
        newPosition.x += translation.x;
        newPosition.y += translation.y;

        bool valid = Grid.IsValidPosition(this, newPosition);

        // Only save the movement if the new position is valid
        if (valid)
        {
            Position = newPosition;
            moveTime = Time.time + moveDelay;
            lockTime = 0f; // reset
        }

        return valid;
    }

    private void Rotate(int direction)
    {
        // Save the current rotation in case the rotation is unsuccessful and needs to be undone
        int originalRotation = RotationIndex;

        // Rotate all the blocks using a rotation matrix
        RotationIndex = Wrap(RotationIndex + direction, 0, 4);
        ApplyRotationMatrix(direction);

        // Revert the rotation if the wall kick tests fail
        if (!TestWallKicks(RotationIndex, direction))
        {
            RotationIndex = originalRotation;
            ApplyRotationMatrix(-direction);
        }
    }

    private void ApplyRotationMatrix(int direction)
    {
        float[] matrix = TetrisGameData.RotationMatrix;

        // Rotate all of the cells using the rotation matrix
        for (int i = 0; i < Cells.Length; i++)
        {
            Vector3 cell = Cells[i];

            int x, y;

            switch (Data.tetromino)
            {
                case Tetromino.I:
                case Tetromino.O:
                    // "I" and "O" are rotated from an offset center point
                    cell.x -= 0.5f;
                    cell.y -= 0.5f;
                    x = Mathf.CeilToInt((cell.x * matrix[0] * direction) + (cell.y * matrix[1] * direction));
                    y = Mathf.CeilToInt((cell.x * matrix[2] * direction) + (cell.y * matrix[3] * direction));
                    break;

                default:
                    x = Mathf.RoundToInt((cell.x * matrix[0] * direction) + (cell.y * matrix[1] * direction));
                    y = Mathf.RoundToInt((cell.x * matrix[2] * direction) + (cell.y * matrix[3] * direction));
                    break;
            }

            Cells[i] = new Vector3Int(x, y, 0);
        }
    }

    private bool TestWallKicks(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = GetWallKickIndex(rotationIndex, rotationDirection);

        for (int i = 0; i < Data.wallKicks.GetLength(1); i++)
        {
            Vector2Int translation = Data.wallKicks[wallKickIndex, i];

            if (Move(translation))
            {
                return true;
            }
        }

        return false;
    }

    private int GetWallKickIndex(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = rotationIndex * 2;

        if (rotationDirection < 0)
        {
            wallKickIndex--;
        }

        return Wrap(wallKickIndex, 0, Data.wallKicks.GetLength(0));
    }

    private int Wrap(int input, int min, int max)
    {
        if (input < min)
        {
            return max - (min - input) % (max - min);
        }
        else
        {
            return min + (input - min) % (max - min);
        }
    }
}
