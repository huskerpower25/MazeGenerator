using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mazeGenerator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public static int rows = 40;
        public static int cols = 40;
        public static int offset = 30;
        public static int h = 800;
        public static int w = 800;

        bool mazeComplete = false;

        int xLoc = 0;
        int yLoc = 0;

        public static int startX;
        public static int startY;
        public static int endX;
        public static int endY;

        PictureBox maze = new PictureBox();        

        cell[,] grid = new cell[rows, cols];

        public static List<lines> borderList = new List<lines>();

        List<cell> cellStack = new List<cell>();

        List<Pathway> allPaths = new List<Pathway>();

        public Random rand = new Random(DateTime.Now.Millisecond);

        SolidBrush visitedBrush = new SolidBrush(Color.LightGray);
        SolidBrush currentBrush = new SolidBrush(Color.CadetBlue);
        SolidBrush startBrush = new SolidBrush(Color.Green);
        SolidBrush endBrush = new SolidBrush(Color.Red);
        SolidBrush pathBrush = new SolidBrush(Color.DarkOliveGreen);
        SolidBrush path1Brush = new SolidBrush(Color.Orange);
        SolidBrush path2Brush = new SolidBrush(Color.Red);
        SolidBrush path3Brush = new SolidBrush(Color.Black);
        SolidBrush path4Brush = new SolidBrush(Color.Blue);
        SolidBrush path5Brush = new SolidBrush(Color.Pink);


        Pen gridPen = new Pen(Color.Black, 2);
        Pen borderPen = new Pen(Color.Black, 4);

        private void updateBitmap()
        {
            maze.Size = new Size(w, h);
            maze.Location = new Point(offset, offset);
            this.Controls.Add(maze);
            Bitmap mazeBitmap = new Bitmap(w, h);
            Graphics mazeGraphics = Graphics.FromImage(mazeBitmap);

            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    foreach (var item in grid[i, j].linesCollection)
                    {
                        mazeGraphics.DrawLine(gridPen, item.x1, item.y1, item.x2, item.y2);
                    }
                    if (grid[i, j].visited)
                    {
                        mazeGraphics.FillRectangle(visitedBrush, grid[i, j].rectLoc);
                    }
                    if (grid[i, j].current)
                    {
                        mazeGraphics.FillRectangle(currentBrush, grid[i, j].rectLoc);
                    }
                    if (grid[i, j].partOfFirstPath)
                    {
                        mazeGraphics.FillRectangle(pathBrush, grid[i, j].rectLoc);
                    }
                    if (grid[i, j].partOfSecondPath)
                    {
                        mazeGraphics.FillRectangle(path1Brush, grid[i, j].rectLoc);
                    }
                    if (grid[i, j].partOfThirdPath)
                    {
                        mazeGraphics.FillRectangle(path2Brush, grid[i, j].rectLoc);
                    }
                    if (grid[i, j].partOfFourthPath)
                    {
                        mazeGraphics.FillRectangle(path3Brush, grid[i, j].rectLoc);
                    }
                    if (grid[i, j].partOfFifthPath)
                    {
                        mazeGraphics.FillRectangle(path3Brush, grid[i, j].rectLoc);
                    }
                }
            }



            //draw start and end
            mazeGraphics.FillRectangle(startBrush, grid[startX, startY].rectLoc);
            mazeGraphics.FillRectangle(endBrush, grid[endX, endY].rectLoc);

            // draw border
            foreach (var item in borderList)
            {
                mazeGraphics.DrawLine(borderPen, item.x1, item.y1, item.x2, item.y2);
            }

            maze.Image = mazeBitmap;
            this.Refresh();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Height = h + offset * 5;
            this.Width = w + offset *2;
            setup();
        }



        #region Button Events
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void btnGo_Click(object sender, EventArgs e)
        {
            if (!tmrEngine.Enabled)
            {
                tmrEngine.Start();
            }
        }
        private void btnReset_Click(object sender, EventArgs e)
        {
            setup();
        }
        private void btnInstant_Click(object sender, EventArgs e)
        {
            instantSolve();
        }
        private void btnPause_Click(object sender, EventArgs e)
        {
            tmrEngine.Stop();
        }
        private void btnSolve_Click(object sender, EventArgs e)
        {
            solveGeneticAlgorithm();
        }
        private void btnSolveAllPaths_Click(object sender, EventArgs e)
        {
            solveAllPaths();
        }
        #endregion

        #region Methods        
        private void setup()
        {
            mazeComplete = false;

            startX = 0;
            startY = 0;

            endX = cols - 1;
            endY = rows - 1;

           // h = this.Height;
           // w = this.Width;


            // make border
            lines topLine = new lines();
            lines bottomLine = new lines();
            lines leftLine = new lines();
            lines rightLine = new lines();

            int x1, x2, y1, y2;

            x1 = 0; // = offset
            y1 = 0; // = offset;
            x2 = h; // + offset;
            y2 = w; // + offset;

            topLine.x1 = x1; topLine.y1 = y1; topLine.x2 = x2; topLine.y2 = y1;
            leftLine.x1 = x1; leftLine.y1 = y1; leftLine.x2 = x1; leftLine.y2 = y2;
            rightLine.x1 = x2; rightLine.y1 = y1; rightLine.x2 = x2; rightLine.y2 = y2;
            bottomLine.x1 = x1; bottomLine.y1 = y2; bottomLine.x2 = x2; bottomLine.y2 = y2;

            borderList.Add(topLine);
            borderList.Add(leftLine);
            borderList.Add(bottomLine);
            borderList.Add(rightLine);

            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    grid[i, j] = new cell();
                    grid[i, j].i = i;
                    grid[i, j].j = j;
                    grid[i, j].create();

                }
            }
            updateBitmap();
        }
        private void erase(int i, int j, cell.wallType walltype)
        {
            for (int k = 0; k < grid[i, j].linesCollection.Count(); k++)
            {
                if (grid[i, j].linesCollection.ElementAt(k).wallType == walltype)
                {
                    grid[i, j].linesCollection.RemoveAt(k);
                    return;
                }
            }
            //grid[i, j].linesCollection.RemoveAll(walltype);

            //foreach (var item in grid[i, j].linesCollection)
            //{
            //    if (item.wallType == walltype)
            //    {
            //        grid[i, j].linesCollection.Remove(item);
            //        return;
            //    }
            //} 
        }
        private cell.pos moveTo(int x, int y)
        {
            cell.pos[] neighbors = getNeighbors(x, y);
            int count = 0;
            int index = 0;
            foreach (var item in neighbors)
            {
                if (item != cell.pos.empty)
                {
                    count++;
                }
            }
            if (count == 0)
            {
                return cell.pos.empty;
            }
            else
            {
                index = rand.Next(0, count);
            }

            Console.WriteLine($"{neighbors[0]}");
            Console.WriteLine($"{neighbors[1]}");
            Console.WriteLine($"{neighbors[2]}");
            Console.WriteLine($"{neighbors[3]}");

            return neighbors[index];
        }
        private void tmrEngine_Tick(object sender, EventArgs e)
        {
            
            cellStack.Add(grid[xLoc, yLoc]);

            if (cellStack.Count > 1)
            {
                cellStack.ElementAt(cellStack.Count - 2).current = false;
            }
            grid[xLoc, yLoc].current = true;
            grid[xLoc, yLoc].visited = true;


            cell.pos moveToPos = moveTo(xLoc, yLoc);
            Console.WriteLine();
            Console.WriteLine(moveToPos.ToString());
            Console.WriteLine($"{xLoc},{yLoc}");

            switch (moveToPos)
            {
                case cell.pos.empty:
                    if (cellStack.Count > 0)
                    {
                        while (cellStack.Count > 1 && moveTo(xLoc,yLoc) == cell.pos.empty)
                        {
                            xLoc = cellStack.ElementAt(cellStack.Count - 2).i;
                            yLoc = cellStack.ElementAt(cellStack.Count - 2).j;
                            cellStack.ElementAt(cellStack.Count - 1).current = false;
                            cellStack.RemoveAt(cellStack.Count - 1);
                        } 
                    }

                    break;
                case cell.pos.above:
                    erase(xLoc, yLoc - 1, cell.wallType.bottomWall);
                    erase(xLoc, yLoc, cell.wallType.topWall);
                    grid[xLoc, yLoc].topWall = false;
                    grid[xLoc, yLoc-1].bottomWall = false;
                    yLoc--;
                    break;
                case cell.pos.left:

                    erase(xLoc - 1, yLoc, cell.wallType.rightWall);
                    erase(xLoc, yLoc, cell.wallType.leftWall);
                    grid[xLoc-1, yLoc].rightWall = false;
                    grid[xLoc, yLoc].leftWall = false;
                    xLoc--;
                    break;
                case cell.pos.below:

                    erase(xLoc, yLoc + 1, cell.wallType.topWall);
                    erase(xLoc, yLoc, cell.wallType.bottomWall);
                    grid[xLoc, yLoc + 1].topWall = false;
                    grid[xLoc, yLoc].bottomWall = false;
                    yLoc++;
                    break;
                case cell.pos.right:

                    erase(xLoc + 1, yLoc, cell.wallType.leftWall);
                    erase(xLoc, yLoc, cell.wallType.rightWall);
                    grid[xLoc + 1, yLoc].leftWall = false;
                    grid[xLoc, yLoc].rightWall = false;
                    xLoc++;
                    break;                
                default:
                    break;
            }
            Console.WriteLine($"{xLoc},{yLoc}");
            Console.WriteLine(cellStack.Count());

            int count = 0;
            foreach (var item in grid)
            {
                if (item.visited)
                {
                    count++;
                }
            }
            if (count == rows*cols)
            {
                mazeComplete = true;
                tmrEngine.Stop();
            }
            updateBitmap();
        }
        public cell.pos[] getNeighbors(int x, int y)
        {
            cell.pos[] neighbors = new cell.pos[4];

            int count = 0;
            // check top
            if (y > 0 && grid[x, y - 1].visited == false)
            {                
                neighbors[count] = cell.pos.above;
                count++;
            }
            
            // check left
            if (x > 0 && grid[x - 1, y].visited == false)
            {
                neighbors[count] = cell.pos.left;
                count++;
            }

            // check bottom 
            if (y < rows-1 && grid[x, y + 1].visited == false)
            {
                neighbors[count] = cell.pos.below;
                count++;
            }

            // check right
            if (x < cols-1 && grid[x + 1, y].visited == false)
            {
                neighbors[count] = cell.pos.right;
            }

            return neighbors;

        }
        private void instantSolve()
        {
            setup();
            int count = 0;
            while (count < rows * cols)
            {
                cellStack.Add(grid[xLoc, yLoc]);

                if (cellStack.Count > 1)
                {
                    cellStack.ElementAt(cellStack.Count - 2).current = false;
                }
                grid[xLoc, yLoc].current = true;
                grid[xLoc, yLoc].visited = true;


                cell.pos moveToPos = moveTo(xLoc, yLoc);
                Console.WriteLine();
                Console.WriteLine(moveToPos.ToString());
                Console.WriteLine($"{xLoc},{yLoc}");

                switch (moveToPos)
                {
                    case cell.pos.empty:
                        if (cellStack.Count > 0)
                        {
                            while (cellStack.Count > 1 && moveTo(xLoc, yLoc) == cell.pos.empty)
                            {
                                xLoc = cellStack.ElementAt(cellStack.Count - 2).i;
                                yLoc = cellStack.ElementAt(cellStack.Count - 2).j;
                                cellStack.ElementAt(cellStack.Count - 1).current = false;
                                cellStack.RemoveAt(cellStack.Count - 1);
                            }
                        }

                        break;
                    case cell.pos.above:
                        erase(xLoc, yLoc - 1, cell.wallType.bottomWall);
                        erase(xLoc, yLoc, cell.wallType.topWall);
                        grid[xLoc, yLoc].topWall = false;
                        grid[xLoc, yLoc - 1].bottomWall = false;
                        yLoc--;
                        break;
                    case cell.pos.left:

                        erase(xLoc - 1, yLoc, cell.wallType.rightWall);
                        erase(xLoc, yLoc, cell.wallType.leftWall);
                        grid[xLoc - 1, yLoc].rightWall = false;
                        grid[xLoc, yLoc].leftWall = false;
                        xLoc--;
                        break;
                    case cell.pos.below:

                        erase(xLoc, yLoc + 1, cell.wallType.topWall);
                        erase(xLoc, yLoc, cell.wallType.bottomWall);
                        grid[xLoc, yLoc + 1].topWall = false;
                        grid[xLoc, yLoc].bottomWall = false;
                        yLoc++;
                        break;
                    case cell.pos.right:

                        erase(xLoc + 1, yLoc, cell.wallType.leftWall);
                        erase(xLoc, yLoc, cell.wallType.rightWall);
                        grid[xLoc + 1, yLoc].leftWall = false;
                        grid[xLoc, yLoc].rightWall = false;
                        xLoc++;
                        break;
                    default:
                        break;
                }
                Console.WriteLine($"{xLoc},{yLoc}");
                Console.WriteLine(cellStack.Count());

                count = 0;
                foreach (var item in grid)
                {
                    if (item.visited)
                    {
                        count++;
                    }
                }
            }
            mazeComplete = true;
            updateBitmap();
        }
        double map(double x, double in_min, double in_max, double out_min, double out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }
        #endregion

        #region Finding All Paths
        private void solveAllPaths()
        {

            Pathway firstPath = new Pathway();
            int[] startPos = new int[2];
            startPos[0] = startX;
            startPos[1] = startY;

            firstPath.path.Add(startPos);

            int x = startPos[0];
            int y = startPos[1];
            grid[x, y].partOfPath = true;

            allPaths.Add(firstPath);

            moveUntilCompleteFirstSolution();

            //moveUntilCompleteAllSolutions(allPaths);
        }       
        private Pathway moveUntilCompleteFirstSolution()
        {
            Pathway firstPathway = new Pathway();
            firstPathway = allPaths.Last();
            bool pathComplete = false;

            while (!pathComplete)
            {

                int[] currentLocation = new int[2];
                int[] nextLocation = new int[2];

                currentLocation = firstPathway.path.Last();

                grid[currentLocation[0], currentLocation[1]].partOfPath = true;
                                
                if (currentLocation[0] == endX && currentLocation[1] == endY)
                {
                    pathComplete = true;
                    break;
                }

                // get all possible moves
                firstPathway.availableMoves =  getPossibleMoves_VisitedNotPossible(currentLocation);

                switch (firstPathway.availableMoves.Count())
                {
                    case 0:
                        bool possibleMoveExists = false;
                        while (!possibleMoveExists)
                        {
                            currentLocation = firstPathway.path.Last();
                            firstPathway.availableMoves = getPossibleMoves_VisitedNotPossible(currentLocation);
                            if (firstPathway.availableMoves.Count() > 0)
                            {
                                possibleMoveExists = true;
                            nextLocation = moveTo_SolveAllPaths(grid[currentLocation[0], currentLocation[1]], firstPathway.availableMoves.ElementAt(0));

                        }

                        else
                            {
                                firstPathway.path.RemoveAt(firstPathway.path.Count()-1);
                            }
                    }
                        break;
                    case 1:
                        nextLocation = moveTo_SolveAllPaths(grid[currentLocation[0], currentLocation[1]], firstPathway.availableMoves.ElementAt(0));
                        break;
                    case 2:
                        nextLocation = moveTo_SolveAllPaths(grid[currentLocation[0], currentLocation[1]], firstPathway.availableMoves.ElementAt(0));
                        break;
                    case 3:
                        nextLocation = moveTo_SolveAllPaths(grid[currentLocation[0], currentLocation[1]], firstPathway.availableMoves.ElementAt(0));
                        break;
                    case 4:
                        nextLocation = moveTo_SolveAllPaths(grid[currentLocation[0], currentLocation[1]], firstPathway.availableMoves.ElementAt(0));
                        break;
                    default:
                        break;
                }

                //nextLocation = moveTo_SolveAllPaths(grid[currentLocation[0], currentLocation[1]], firstPathway.availableMoves.ElementAt(0));
                firstPathway.availableMoves.Clear();
                firstPathway.path.Add(nextLocation);
                Console.WriteLine($"\n current: ({currentLocation[0]},{currentLocation[1]}) next: ({nextLocation[0]}, {nextLocation[1]})\n");
            }

            foreach (var item in firstPathway.path) 
            {
                grid[item[0], item[1]].partOfFirstPath = true;
            }
            updateBitmap();
            return firstPathway;
        }
        private List<Pathway.movement> getPossibleMoves_VisitedNotPossible(int[] currentLocation)
        {
            List<Pathway.movement> availableMoves = new List<Pathway.movement>();

            if (!grid[currentLocation[0], currentLocation[1]].topWall && !grid[currentLocation[0], currentLocation[1] - 1].partOfPath) // if this fails, move to nested if loop
            {
                availableMoves.Add(Pathway.movement.up);
            }

            if (!grid[currentLocation[0], currentLocation[1]].leftWall && !grid[currentLocation[0] - 1, currentLocation[1]].partOfPath) // if this fails, move to nested if loop
            {
                availableMoves.Add(Pathway.movement.left);
            }

            if (!grid[currentLocation[0], currentLocation[1]].rightWall && !grid[currentLocation[0] + 1, currentLocation[1]].partOfPath) // if this fails, move to nested if loop
            {
                availableMoves.Add(Pathway.movement.right);
            }

            if (!grid[currentLocation[0], currentLocation[1]].bottomWall && !grid[currentLocation[0], currentLocation[1] + 1].partOfPath) // if this fails, move to nested if loop
            {
                availableMoves.Add(Pathway.movement.down);
            }

            return availableMoves;            
        }
        private int[] moveTo_SolveAllPaths(cell currentCell, Pathway.movement next)
        {
            int[] newLocation = new int[3];

            newLocation[0] = currentCell.i;
            newLocation[1] = currentCell.j;
            newLocation[2] = 0;

            switch (next)
            {
                case Pathway.movement.empty:
                    break;
                case Pathway.movement.left:
                    newLocation[0]--;
                    break;
                case Pathway.movement.right:
                    newLocation[0]++;
                    break;
                case Pathway.movement.up:
                    newLocation[1]--;
                    break;
                case Pathway.movement.down:
                    newLocation[1]++;
                    break;
                case Pathway.movement.stay:
                    break;
                default:
                    break;
            }

            return newLocation;
        }
        #endregion

        #region Genetic Algorithm
        private void solveGeneticAlgorithm()
        {
            if (!mazeComplete)
            {
                instantSolve();
            }

            int populationSize = 10;
            int totalMoves = 250;
            int genCount = 5;
            double mutateChance = 0.2;

            List<Population> p = new List<Population>();
            List<Population> maxFitList = new List<Population>();


            // initialize population
            for (int i = 0; i < populationSize; i++)
            {
                p.Add(new Population());
                p.ElementAt(i).init(totalMoves);
            }


            for (int ct = 0; ct < genCount; ct++)
            {
                // calculate fitness for population
                double minFitness = double.PositiveInfinity;
                double maxFitness = 0;
                int maxFitnessIndex = 0;
                for (int i = 0; i < populationSize; i++)
                {
                    p[i].fitness = calcFitness_AllMovesPossible(p[i], totalMoves);
                    if (p[i].fitness > maxFitness)
                    {
                        maxFitness = p[i].fitness;
                        maxFitnessIndex = i;
                    }
                    if (p[i].fitness < minFitness)
                    {
                        minFitness = p[i].fitness;
                    }
                }
                maxFitList.Add(p[maxFitnessIndex]);
                // normalize fitness
                for (int i = 0; i < populationSize; i++)
                {
                    p[i].fitness = map(p[i].fitness, minFitness, maxFitness, 0, 1);
                    Console.WriteLine(p[i].fitness);
                }


                List<Population> matingPool = new List<Population>();
                // add population to mating pool
                for (int i = 0; i < populationSize; i++)
                {
                    double toAdd = p[i].fitness * 1000;
                    for (int j = 0; j < (int)toAdd; j++)
                    {
                        matingPool.Add(p[i]);
                    }
                }

                // clear current population
                p.Clear();

                // pull from mating pool
                for (int i = 0; i < populationSize; i++)
                {
                    Population parent1 = new Population();
                    Population parent2 = new Population();
                    Population child = new Population();

                    int[] startPos = { startX, startY };

                    parent1 = matingPool.ElementAt(rand.Next(0, matingPool.Count - 1));
                    parent2 = matingPool.ElementAt(rand.Next(0, matingPool.Count - 1));
                    child.path.Add(startPos);

                    for (int j = 1; j < totalMoves; j++)
                    {
                        // need movement and location

                        Pathway.movement movement = new Pathway.movement();
                        int[] location = new int[2];

                        double whichParent = rand.NextDouble();
                        if (whichParent < (1 - mutateChance) / 2)
                        {
                            location = moveCells_GA(child.path[j-1], parent1.move[j]);
                            movement = parent1.move[j];

                        }
                        else if (whichParent > (1 - mutateChance) / 2 && whichParent < 1 - mutateChance)
                        {
                            location = moveCells_GA(child.path[j-1], parent2.move[j]);
                            movement = parent2.move[j];
                        }
                        else
                        {
                            var t = child.moveToNextCell_AllMovesPossible(child.path.Last());

                            child.path.Add(t.Item1);
                            child.move.Add(t.Item2);
                        }

                        child.path.Add(location);
                        child.move.Add(movement);

                    }
                    Console.WriteLine($"{parent1.fitness},{parent1.fitness}");
                    p.Add(child);
                }




                // check randomness?
                for (int i = 0; i < populationSize; i++)
                {
                    int x = 0, y = 0;
                    for (int j = 0; j < totalMoves; j++)
                    {
                        x = x + p[i].path[j][0];
                        y = y + p[i].path[j][1];
                    }
                    Console.WriteLine(x + y);
                }

                // update bitmap
                switch (ct)
                {
                    case 0:
                        foreach (var item in maxFitList)
                        {
                            for (int i = 0; i < item.path.Count(); i++)
                            {
                                grid[item.path[i][0], item.path[i][1]].partOfFirstPath = true;
                            }
                        }
                        break;
                    case 1:
                        foreach (var item in maxFitList)
                        {
                            for (int i = 0; i < item.path.Count(); i++)
                            {
                                grid[item.path[i][0], item.path[i][1]].partOfSecondPath = true;
                            }
                        }
                        break;
                    case 2:
                        foreach (var item in maxFitList)
                        {
                            for (int i = 0; i < item.path.Count(); i++)
                            {
                                grid[item.path[i][0], item.path[i][1]].partOfSecondPath = true;
                            }
                        }
                        break;
                    case 3:
                        foreach (var item in maxFitList)
                        {
                            for (int i = 0; i < item.path.Count(); i++)
                            {
                                grid[item.path[i][0], item.path[i][1]].partOfThirdPath = true;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            updateBitmap();
        }
        private int[] moveCells_GA(int[] cell, Pathway.movement move)
        {
            switch (move)
            {
                case Pathway.movement.empty:
                    break;
                case Pathway.movement.left:
                    cell[0]--;
                    break;
                case Pathway.movement.right:
                    cell[0]++;
                    break;
                case Pathway.movement.up:
                    cell[1]--;
                    break;
                case Pathway.movement.down:
                    cell[1]++;
                    break;
                case Pathway.movement.stay:
                    break;
                default:
                    break;
            }

            return cell;
        }
        private int[] mutate(Population child)
        {
            List<Pathway.movement> availableMoves = new List<Pathway.movement>();

            int[] newLoc = new int[2];
            newLoc = child.path.Last();

            if (newLoc[0] != 0)
            {
                availableMoves.Add(Pathway.movement.left);
            }
            if (newLoc[0] != cols)
            {
                availableMoves.Add(Pathway.movement.right);
            }
            if (newLoc[1] != 0)
            {
                availableMoves.Add(Pathway.movement.up);
            }
            if (newLoc[1] != rows)
            {
                availableMoves.Add(Pathway.movement.down);
            }

            int move = rand.Next(0, availableMoves.Count() - 1);

            Pathway.movement movement = availableMoves[move];

            switch (movement)
            {
                case Pathway.movement.empty:
                    break;
                case Pathway.movement.left:
                    newLoc[0]--;
                    break;
                case Pathway.movement.right:
                    newLoc[0]++;
                    break;
                case Pathway.movement.up:
                    newLoc[1]--;
                    break;
                case Pathway.movement.down:
                    newLoc[1]++;
                    break;
                case Pathway.movement.stay:
                    break;
                default:
                    break;
            }

            return newLoc;
        }
        private List<Population> runGeneration(List<Population> p, int populationSize, int totalMoves)
        {




            return p;
        }
        public double calcFitness_AllMovesPossible(Population p, int totalMoves)
        {
            double fitness = 0;
            int wallsHit = 0;
            int distToEnd = rows + cols;


            // calc walls hit
            //for (int i = 0; i < totalMoves; i++)
            //{
            //    if (hitwall)
            //    {
            //        wallsHit++;
            //    }
            //}

            // calc final distance to end
            int i;
            int j;

            i = p.path.Last()[0];
            j = p.path.Last()[1];
            distToEnd = (cols - i) + (rows - j);

            fitness = 1d / distToEnd;
            
            return fitness;
        }
        #endregion
    }

    public class cell
    {
        public int i;
        public int j;

        public bool visited = false;
        public bool current = false;

        public int formWidth;
        public int formHeight;
        int width;
        int height;

        public bool topWall = true;
        public bool leftWall = true;
        public bool bottomWall = true;
        public bool rightWall = true;

        public Rectangle rectLoc = new Rectangle();

        public int offset;

        public List<cell> neighborList = new List<cell>();

        public List<lines> linesCollection = new List<lines>();

        public bool partOfPath = false;
        public bool partOfFirstPath = false;
        public bool partOfSecondPath = false;
        public bool partOfThirdPath = false;
        public bool partOfFourthPath = false;
        public bool partOfFifthPath = false;


        public enum pos
        {
            empty,
            above,
            left,
            below,
            right,            
        }

        public enum wallType
        {
            topWall,
            bottomWall,
            leftWall,
            rightWall
        }

        public void create()
        {
            lines topLine = new lines();
            lines bottomLine = new lines();
            lines leftLine = new lines();
            lines rightLine = new lines();

            offset = Form1.offset;


            width = (Form1.borderList.ElementAt(0).x2 - Form1.borderList.ElementAt(0).x1) / Form1.cols;
            height = (Form1.borderList.ElementAt(1).y2 - Form1.borderList.ElementAt(1).y1) / Form1.rows;

            if (width * Form1.cols > height * Form1.rows)
            {
                width = height;
            }
            else
            {
                height = width;
            }

            int x1, x2, y1, y2;

            x1 = (i * width);
            y1 = (j * height);
            x2 = (i * width) + width;
            y2 = (j * height) + height;

            rectLoc.X = (x1);// + 1);
            rectLoc.Y = (y1);// + 1);
            rectLoc.Width = width;// - 1;
            rectLoc.Height = width;// - 1;

            topLine.wallType = wallType.topWall;
            bottomLine.wallType = wallType.bottomWall;
            leftLine.wallType = wallType.leftWall;
            rightLine.wallType = wallType.rightWall;

            topLine.x1 = x1;
            topLine.y1 = y1;
            topLine.x2 = x2;
            topLine.y2 = y1;

            leftLine.x1 = x1;
            leftLine.y1 = y1;
            leftLine.x2 = x1;
            leftLine.y2 = y2;

            rightLine.x1 = x2;
            rightLine.y1 = y1;
            rightLine.x2 = x2;
            rightLine.y2 = y2;

            bottomLine.x1 = x1;
            bottomLine.y1 = y2;
            bottomLine.x2 = x2;
            bottomLine.y2 = y2;

            linesCollection.Add(topLine);
            linesCollection.Add(leftLine);
            linesCollection.Add(bottomLine);
            linesCollection.Add(rightLine);
        }
    }
    public class lines
    {
        public cell.wallType wallType;        

        public int x1;
        public int y1;
        public int x2;
        public int y2;
    }


    public class Pathway
    {
        public List<int[]> path = new List<int[]>();
        public bool finished = false;
        public movement lastPathChoice = movement.empty; // 0 = empty, 1 = left, 2 = right, 3 = up, 4 = down, 5 = stay
        public List<movement> availableMoves = new List<movement>();
        public int movementChoice = 0;
        public enum movement
        {
            empty,
            left,
            right,
            up,
            down,
            stay
        }
    }

    public class Population
    {
        public List<int[]> path = new List<int[]>();
        public List<Pathway.movement> move = new List<Pathway.movement>();

        int[] currentCell = new int[2];

        public double fitness = 0;

        Random r = new Random(DateTime.Now.Millisecond);

        enum next
        {
            empty,
            left,
            right,
            up,
            down,
            stay
        }

        // add cell to path
        // move to next cell
         
        public void init(int size)
        {
            currentCell[0] = Form1.startX;
            currentCell[0] = Form1.startY;


            for (int i = 0; i < size; i++)
            {
                var tuple = moveToNextCell_AllMovesPossible(currentCell);
                currentCell = tuple.Item1;
                path.Add(currentCell);
                move.Add(tuple.Item2);
                                

                if (currentCell[0] == Form1.endX && currentCell[i] == Form1.endY)
                {
                    //stay here
                }
            }
        }
        
        // give a cell, move to the next based on previous

        public Tuple<int[], Pathway.movement> moveToNextCell_AllMovesPossible(int[] currentCell) // currentCell, Pathway.movement movement)
        {
            int[] newCell = new int[2];

            int i = currentCell[0];
            int j = currentCell[1];
            bool possibleMove = false;
            next nextCell = new next();
            Pathway.movement toMove = new Pathway.movement();

            while (!possibleMove)
            {
                nextCell = (next)r.Next(1, 5);

                switch (nextCell)
                {
                    case next.empty:
                        break;
                    case next.left:
                        toMove = Pathway.movement.left;
                        i--;
                        break;
                    case next.right:
                        toMove = Pathway.movement.right;
                        i++;
                        break;
                    case next.up:
                        toMove = Pathway.movement.up;
                        j--;
                        break;
                    case next.down:
                        toMove = Pathway.movement.down;
                        j++;
                        break;
                    default:
                        break;
                }

                if (i >= 0 && i <= Form1.cols - 1 && j >= 0 && j <= Form1.rows - 1)
                {
                    possibleMove = true;
                }
                else
                {
                    i = currentCell[0];
                    j = currentCell[1];
                }
            }
            int[] newPos = new int[2];
            newPos[0] = i;
            newPos[1] = j;
            var tuple = Tuple.Create(newPos, toMove);
            return tuple;
        }
            


    }
}
