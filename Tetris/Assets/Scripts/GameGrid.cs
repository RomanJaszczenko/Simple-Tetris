using UnityEngine;
using UnityEngine.Tilemaps;

public class GameGrid : MonoBehaviour
{
    public Tilemap tilemap { get; private set; }
    public BlocksController activeBlock { get; private set; }

    public TetrominoData[] tetrominoes;
    public Vector2Int boardSize = new Vector2Int(10, 20);
    public Vector3Int spawnPosition = new Vector3Int(-1, 8, 0);

    public RectInt Bounds {
        get
        {
            Vector2Int position = new Vector2Int(-boardSize.x / 2, -boardSize.y / 2);
            return new RectInt(position, boardSize);
        }
    }

    private void Awake()
    {
        tilemap = GetComponentInChildren<Tilemap>();
        activeBlock = GetComponentInChildren<BlocksController>();

        for (int i = 0; i < tetrominoes.Length; i++) {
            tetrominoes[i].Initialize();
        }
    }

    private void Start()
    {
        SpawnPiece();
    }

    public void SpawnPiece()
    {
        int random = Random.Range(0, tetrominoes.Length);
        TetrominoData data = tetrominoes[random];

        activeBlock.Initialize(this, spawnPosition, data);

        if (IsValidPosition(activeBlock, spawnPosition)) {
            Set(activeBlock);
        } else {
            GameOver();
        }
    }

    public void GameOver()
    {
        tilemap.ClearAllTiles();

        // Continue
    }

    public void Set(BlocksController block)
    {
        for (int i = 0; i < block.Cells.Length; i++)
        {
            Vector3Int tilePosition = block.Cells[i] + block.Position;
            tilemap.SetTile(tilePosition, block.Data.tile);
        }
    }

    public void Clear(BlocksController block)
    {
        for (int i = 0; i < block.Cells.Length; i++)
        {
            Vector3Int tilePosition = block.Cells[i] + block.Position;
            tilemap.SetTile(tilePosition, null);
        }
    }

    public bool IsValidPosition(BlocksController block, Vector3Int position)
    {
        RectInt bounds = Bounds;

        // The position is considered valid only if each block is in a valid position
        for (int i = 0; i < block.Cells.Length; i++)
        {
            Vector3Int tilePosition = block.Cells[i] + position;

            // A tile outside the boundaries is considered invalid
            if (!bounds.Contains((Vector2Int)tilePosition)) {
                return false;
            }

            // The position is invalid as it is already occupied by another tile
            if (tilemap.HasTile(tilePosition)) {
                return false;
            }
        }

        return true;
    }

    public void ClearLines()
    {
        RectInt bounds = Bounds;
        int row = bounds.yMin;

        // full clear
        while (row < bounds.yMax)
        {
            // Proceed to the next row only if the current row is not cleared,
            // as the tiles above will fall down when a row is cleared
            if (IsLineFull(row)) {
                LineClear(row);
            } else {
                row++;
            }
        }
    }

    public bool IsLineFull(int row)
    {
        RectInt bounds = Bounds;

        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            Vector3Int position = new Vector3Int(col, row, 0);

            // The line is not full if a tile is missing
            if (!tilemap.HasTile(position)) {
                return false;
            }
        }

        return true;
    }

    public void LineClear(int row)
    {
        RectInt bounds = Bounds;

        // Clear all tiles in the row
        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            Vector3Int position = new Vector3Int(col, row, 0);
            tilemap.SetTile(position, null);
        }

        // Shift every row above downward by one
        while (row < bounds.yMax)
        {
            for (int col = bounds.xMin; col < bounds.xMax; col++)
            {
                Vector3Int position = new Vector3Int(col, row + 1, 0);
                TileBase above = tilemap.GetTile(position);

                position = new Vector3Int(col, row, 0);
                tilemap.SetTile(position, above);
            }

            row++;
        }
    }
}
