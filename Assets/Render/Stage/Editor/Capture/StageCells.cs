using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.Render.Stage
{
    // The board of the captured scene, read the way the captures place and aim: on the player's side
    public static class StageCells
    {
        public static Board Board(RenderManager manager)
        {
            return manager.entityManager.board;
        }

        public static float Size(RenderManager manager)
        {
            return Board(manager).layout.cellSize;
        }

        // Where a player unit dropped at the position stands: its cell, or the nearest free cell of the player's side
        public static Vector3 Nearest(RenderManager manager, Vector3 position)
        {
            Board(manager).TryGetPlacementPosition(position, BoardCellType.Player, out Vector3 placement);
            return placement;
        }

        // A cell of a side a unit could stand on: not blocked, nobody on it
        public static bool IsOpen(Board board, Vector2Int coord)
        {
            BoardCellType side = board.GetCellType(coord);
            return side != BoardCellType.None && board.IsFree(coord, side);
        }

        // Every cell of the board, column after column
        public static IEnumerable<Vector2Int> Coords(Board board)
        {
            for (int x = 0; x < board.layout.width; x++)
            {
                for (int y = 0; y < board.layout.height; y++)
                {
                    yield return new Vector2Int(x, y);
                }
            }
        }
    }
}
