using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Unity.VisualScripting;
using TMPro;
using UnityEngine.UI;

//TODO? make that you can not only have 20x20 or 30x30 but also 20x30 or 12x24
//TODO make that you can have probability so that some tiles are a bit more likely
//TODO if you wanna boost perfomance unity burst for somebody help from 3.5s -> 0.25s

public class WaveFunctionCollapse2 : MonoBehaviour
{
    public int dimensions;
    public Tile[] tileObjects;
    public List<Cell> gridComponents;
    public Cell cellObj;

    public Tile backupTile;

    private int iteration;
    private int successes = 0;
    private int totalAttempts = 0;
    private float currentRunTime = 0;
    private float highestRunTime = 0;
    private float lowestRunTime = 0;

    public TextMeshProUGUI textElement;
    public TextMeshProUGUI timerTextElement;
    public TextMeshProUGUI timerTextElement2;
    public Toggle toggle;

    private void Awake()
    {
        if(isActiveAndEnabled){
            gridComponents = new List<Cell>();
            SetTextElement();
            InitializeGrid();
            SetTimerTextElement();
        }
    }

    private void Update(){
        currentRunTime += Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.R))
        {
            totalAttempts++;
            Restart();
            currentRunTime = 0;
        }
    }

    void InitializeGrid()
    {
        int count = 0;
        for(int y = 0; y < dimensions; y++)
        {
            for(int x = 0; x < dimensions; x++)
            {
                Cell newCell = Instantiate(cellObj, new Vector3(x, 0, y), Quaternion.identity);
                newCell.CreateCell(false, tileObjects);
                gridComponents.Add(newCell);
                newCell.number = count;
                count++;
            }
        }

        StartCoroutine(CheckEntropy());
    }

    IEnumerator CheckEntropy()
    {
        List<Cell> tempGrid = new List<Cell>(gridComponents);
        tempGrid.RemoveAll(c => c.collapsed);
        tempGrid.Sort((a, b) => a.tileOptions.Length - b.tileOptions.Length);
        tempGrid.RemoveAll(a => a.tileOptions.Length != tempGrid[0].tileOptions.Length);

        yield return new WaitForSeconds(0.025f);

        CollapseCell(tempGrid);
    }

    void CollapseCell(List<Cell> tempGrid)
    {
        int randIndex = UnityEngine.Random.Range(0, tempGrid.Count);
        bool restart = false;

        Cell cellToCollapse = tempGrid[randIndex];

        cellToCollapse.collapsed = true;
        try
        {
            Tile selectedTile = SelectRandomTile(cellToCollapse.tileOptions);
            cellToCollapse.tileOptions = new Tile[] { selectedTile };
        }
        catch
        {
            restart = true;
            Tile selectedTile = backupTile;
            cellToCollapse.tileOptions = new Tile[] { selectedTile };
        }

        Tile foundTile = cellToCollapse.tileOptions[0];
        Tile generatedTile = Instantiate(foundTile, cellToCollapse.transform.position, foundTile.transform.rotation);
        cellToCollapse.setTile = generatedTile;

        if(!restart){
            UpdateGeneration(cellToCollapse.number);
        }else{
            totalAttempts++;
            Restart();
            currentRunTime = 0;
        }
    }

    //TODO? not go through every cell but only through cells neighbouring the collapsed cell and if a cell modified their neighbours ...
    //TODO ABOVE especially for performance
    void UpdateGeneration(int indexWhereCellCollapsed)
    {

        //Cell collapsedCell = gridComponents[indexWhereCellCollapsed];

        //1. Check if up/down/left/right is collapse
        //2. If not go where it is not collapsed and eliminate now impossible options
        //3. Continue going in not collapsed cells until nothing in possibilities has changed

        //Make method that checks for cell at index that checks if up/down/right/left is collapsed or if options would change when compared
        //If not collapsed and would change jump into method again with said cell

        List<Tile> tileOptions = new List<Tile>();
        tileOptions = gridComponents[indexWhereCellCollapsed].tileOptions.ToList();

        CheckNeighbours(indexWhereCellCollapsed, tileOptions);



        /*List<Cell> newGenerationCell = new List<Cell>(gridComponents);

        for(int y = 0; y < dimensions; y++)
        {
            for(int x = 0; x < dimensions; x++)
            {
                var index = x + y * dimensions;

                if (gridComponents[index].collapsed)
                {
                    newGenerationCell[index] = gridComponents[index];
                }
                else
                {
                    List<Tile> options = new List<Tile>();
                    //Besser ?
                    options = gridComponents[index].tileOptions.ToList();
                    foreach(Tile t in tileObjects)
                    {
                        options.Add(t);
                    }

                    if(y > 0)
                    {
                        Cell up = gridComponents[x + (y - 1) * dimensions];
                        List<Tile> validOptions = new List<Tile>();

                        foreach(Tile possibleOptions in up.tileOptions)
                        {
                            var validOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[validOption].downNeighbours;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    if(x < dimensions - 1)
                    {
                        Cell left = gridComponents[x + 1 + y * dimensions];
                        List<Tile> validOptions = new List<Tile>();

                        foreach(Tile possibleOptions in left.tileOptions)
                        {
                            var validOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[validOption].rightNeighbours;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    if (y < dimensions - 1)
                    {
                        Cell down = gridComponents[x + (y+1) * dimensions];
                        List<Tile> validOptions = new List<Tile>();

                        foreach (Tile possibleOptions in down.tileOptions)
                        {
                            var validOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[validOption].upNeighbours;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    if (x > 0)
                    {
                        Cell right = gridComponents[x - 1 + y * dimensions];
                        List<Tile> validOptions = new List<Tile>();

                        foreach (Tile possibleOptions in right.tileOptions)
                        {
                            var validOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[validOption].leftNeighbours;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    Tile[] newTileList = new Tile[options.Count];

                    for(int i = 0; i < options.Count; i++) {
                        newTileList[i] = options[i];
                    }

                    newGenerationCell[index].RecreateCell(newTileList);
                }
            }
        }

        gridComponents = newGenerationCell;*/
        iteration++;

        if (iteration < dimensions * dimensions)
        {
            StartCoroutine(CheckEntropy());
        }else{
            successes++;
            totalAttempts++;
            if(toggle.isOn) Restart();
            SetTimerTextElement();
            currentRunTime = 0;
        }
    }

    private void CheckNeighbours(int indexWhereCellCollapsed, List<Tile> tileOptions)
    {
        /*Debug.Log("HasCellUp: " + HasCellUp(indexWhereCellCollapsed) + " GetIndexUp: " + GetIndexUp(indexWhereCellCollapsed)
                + "\nHasCellDown: " + HasCellDown(indexWhereCellCollapsed) + " GetIndexDown: " + GetIndexDown(indexWhereCellCollapsed)
                + "\nHasCellRight: " + HasCellRight(indexWhereCellCollapsed) + " GetIndexRight: " + GetIndexRight(indexWhereCellCollapsed)
                + "\nHasCellLeft: " + HasCellLeft(indexWhereCellCollapsed) + " GetIndexLeft: " + GetIndexLeft(indexWhereCellCollapsed));*/
        if(HasCellUp(indexWhereCellCollapsed)){
            int indexUp = GetIndexUp(indexWhereCellCollapsed);
            SetTilesUpCell(indexWhereCellCollapsed, tileOptions);
        }

        if(HasCellDown(indexWhereCellCollapsed)){
            int indexDown = GetIndexDown(indexWhereCellCollapsed);
            SetTilesDownCell(indexWhereCellCollapsed, tileOptions);
        }

        if(HasCellRight(indexWhereCellCollapsed)){
            int indexRight = GetIndexRight(indexWhereCellCollapsed);
            SetTilesRightCell(indexWhereCellCollapsed, tileOptions);
        }

        if(HasCellLeft(indexWhereCellCollapsed)){
            int indexLeft = GetIndexLeft(indexWhereCellCollapsed);
            SetTilesLeftCell(indexWhereCellCollapsed, tileOptions);
        }

    }

    void CheckValidity(List<Tile> optionList, List<Tile> validOption)
    {
        for(int x = optionList.Count - 1; x >=0; x--)
        {
            var element = optionList[x];
            if (!validOption.Contains(element))
            {
                optionList.RemoveAt(x);
            }
        }
    }

    #region HelperMethods
    private void Restart()
    {
        SetTextElement();
        StopAllCoroutines();
        for (int i = gridComponents.Count - 1; i >= 0; i--)
        {
            Cell cell = gridComponents[i];
            if (cell.collapsed)
            {
                Destroy(cell.setTile.gameObject);
            }
            Destroy(cell.gameObject);
            gridComponents.RemoveAt(i);
        }
        iteration = 0;
        InitializeGrid();
    }

    private void SetTextElement()
    {
        if (totalAttempts <= 250)
        {
            float successPercentage = totalAttempts > 0 ? (successes / (float)totalAttempts) * 100 : 0;
            textElement.text = $"Success: {successPercentage:F2}%\nAttempts: {totalAttempts}";
        }

    }

    private void SetTimerTextElement()
    {
        if (highestRunTime <= currentRunTime)
        {
            int minutes = Mathf.FloorToInt(currentRunTime / 60F);
            int seconds = Mathf.FloorToInt(currentRunTime % 60F);
            int milliseconds = Mathf.FloorToInt((currentRunTime * 100F) % 100F);

            timerTextElement.text = string.Format("Highest Run Time: {0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
            highestRunTime = currentRunTime;
        }

        if (lowestRunTime >= currentRunTime || lowestRunTime == 0)
        {
            int minutes = Mathf.FloorToInt(currentRunTime / 60F);
            int seconds = Mathf.FloorToInt(currentRunTime % 60F);
            int milliseconds = Mathf.FloorToInt((currentRunTime * 100F) % 100F);

            timerTextElement2.text = string.Format("Lowest Run Time: {0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
            lowestRunTime = currentRunTime;
        }
    }

    public Tile SelectRandomTile(Tile[] tileOptions)
    {
        List<Tile> weightedTileList = new List<Tile>();

        foreach (Tile tile in tileOptions)
        {
            for (int i = 0; i < tile.probability; i++)
            {
                weightedTileList.Add(tile);
            }
        }

        int randomIndex = UnityEngine.Random.Range(0, weightedTileList.Count);
        return weightedTileList[randomIndex];
    }

    public bool HasCellDown(int index)
    {
        int row = index / dimensions;
        return row > 0;
    }

    public bool HasCellUp(int index)
    {
        int row = index / dimensions;
        return row < dimensions - 1;
    }

    public bool HasCellLeft(int index)
    {
        int col = index % dimensions;
        return col > 0;
    }

    public bool HasCellRight(int index)
    {
        int col = index % dimensions;
        return col < dimensions - 1;
    }

    public int GetIndexUp(int index)
    {
        if (HasCellUp(index))
        {
            return index + dimensions;
        }
        return -1;
    }

    public int GetIndexDown(int index)
    {
        if (HasCellDown(index))
        {
            return index - dimensions;
        }
        return -1;
    }

    public int GetIndexLeft(int index)
    {
        if (HasCellLeft(index))
        {
            return index - 1;
        }
        return -1;
    }

    public int GetIndexRight(int index)
    {
        if (HasCellRight(index))
        {
            return index + 1;
        }
        return -1;
    }
    #endregion

    private void SetTilesUpCell(int index, List<Tile> options)
    {
        Cell up = gridComponents[GetIndexUp(index)];
        HashSet<Tile> validUpNeighbours = new HashSet<Tile>();
    
        foreach (Tile option in options)
        {
            foreach (Tile upNeighbour in option.downNeighbours)
            {
                validUpNeighbours.Add(upNeighbour);
            }
        }
    
        List<Tile> newUpTileOptions = new List<Tile>();
        foreach (Tile tileOption in up.tileOptions)
        {
            if (validUpNeighbours.Contains(tileOption))
            {
                newUpTileOptions.Add(tileOption);
            }
        }
    
        up.tileOptions = newUpTileOptions.ToArray();
    }

    private void SetTilesDownCell(int index, List<Tile> options)
    {
        Cell down = gridComponents[GetIndexDown(index)];
        HashSet<Tile> validDownNeighbours = new HashSet<Tile>();
    
        foreach (Tile option in options)
        {
            foreach (Tile downNeighbour in option.upNeighbours)
            {
                validDownNeighbours.Add(downNeighbour);
            }
        }
    
        List<Tile> newDownTileOptions = new List<Tile>();
        foreach (Tile tileOption in down.tileOptions)
        {
            if (validDownNeighbours.Contains(tileOption))
            {
                newDownTileOptions.Add(tileOption);
            }
        }
    
        down.tileOptions = newDownTileOptions.ToArray();
    }

    private void SetTilesRightCell(int index, List<Tile> options)
    {
        Cell right = gridComponents[GetIndexRight(index)];
        HashSet<Tile> validRightNeighbours = new HashSet<Tile>();
    
        foreach (Tile option in options)
        {
            foreach (Tile rightNeighbour in option.leftNeighbours)
            {
                validRightNeighbours.Add(rightNeighbour);
            }
        }
    
        List<Tile> newRightTileOptions = new List<Tile>();
        foreach (Tile tileOption in right.tileOptions)
        {
            if (validRightNeighbours.Contains(tileOption))
            {
                newRightTileOptions.Add(tileOption);
            }
        }
    
        right.tileOptions = newRightTileOptions.ToArray();
    }

    private void SetTilesLeftCell(int index, List<Tile> options)
    {
        Cell left = gridComponents[GetIndexLeft(index)];
        HashSet<Tile> validLeftNeighbours = new HashSet<Tile>();
    
        foreach (Tile option in options)
        {
            foreach (Tile leftNeighbour in option.rightNeighbours)
            {
                validLeftNeighbours.Add(leftNeighbour);
            }
        }
    
        List<Tile> newleftTileOptions = new List<Tile>();
        foreach (Tile tileOption in left.tileOptions)
        {
            if (validLeftNeighbours.Contains(tileOption))
            {
                newleftTileOptions.Add(tileOption);
            }
        }
    
        left.tileOptions = newleftTileOptions.ToArray();
    }
}
