using System.Collections.Generic;
using System;

public class SurroundingMap
{
   
    public static Dictionary<int, List<int>> CreateSurroundingTilesDictionary(int Columns, int Rows, int Radius)
    {
      

        var surroundingTilesDict = new Dictionary<int, List<int>>();

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                int tileIndex = GetTileIndex(row, col, Columns);
                var surroundingTiles = GetSurroundingTiles(row, col,Radius, Columns, Rows );
                surroundingTilesDict[tileIndex] = surroundingTiles;
            }
        }

        return surroundingTilesDict;
    }

    private static int GetTileIndex(int row, int col, int Columns)
    {
        return row * Columns + col;
    }

    private static List<int> GetSurroundingTiles(int row, int col, int Radius, int Columns, int Rows)
    {
        var surroundingTiles = new List<int>();

        int rowStart = Math.Max(0, row - Radius);
        int rowEnd = Math.Min(Rows - 1, row + Radius);
        int colStart = Math.Max(0, col - Radius);
        int colEnd = Math.Min(Columns - 1, col + Radius);

        for (int newRow = rowStart; newRow <= rowEnd; newRow++)
        {
            for (int newCol = colStart; newCol <= colEnd; newCol++)
            {
                if (!(newRow == row && newCol == col))
                {
                    surroundingTiles.Add(GetTileIndex(newRow, newCol, Columns));
                }
            }
        }

        return surroundingTiles;
    }

    //private static bool IsValidTile(int row, int col)
    //{
    //    return row >= 0 && row < Rows && col >= 0 && col < Columns;
    //}
}

