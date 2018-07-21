using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//1     - X префаб
//0     - O префаб
//-10   - пусто
namespace Assets.Scripts
{
	public class VectorInt
	{
		public int X;
		public int Y;
	}

	public class TicTacToe : MonoBehaviour
	{
		private const int BoardSize = 3;
		private int[,] board = new int[BoardSize, BoardSize];
		private GameObject[,] boardGameObject = new GameObject[BoardSize, BoardSize];
        private ClickCube clickCube;
        private int x_oPrefab = 0;                                                           //координата X при нажатии на поле
        private int z_oPrefab = 0;                                                           //координата Z при нажатии на поле
       

        [SerializeField] private GameObject xPrefab;
		[SerializeField] private GameObject oPrefab;

		[SerializeField]
		private Text uiText;

		private float updateInterval = 1.0f;
		private float currentInterval = 0.0f;

		private int iterator;
		private int currentState;

		public void Awake()
		{
			Restart();
            clickCube = FindObjectOfType<ClickCube>();
        }

		public void Restart()
		{
			for (int i = 0; i < BoardSize; i++)
			{
				for (int j = 0; j < BoardSize; j++)
				{

					board[i, j] = -10;
					if (boardGameObject[i, j] != null)
					{
						Destroy(boardGameObject[i, j]);
					}
				}
			}
		}

		private int GetCount(int value)
		{
			int count = 0;
			for (int i = 0; i < BoardSize; i++)
			{
				for (int j = 0; j < BoardSize; j++)
				{
					if(board[i, j] == value)
					{
						count++;
					}
				}
			}
			return count;
		}

		private void WaitMove()
		{
			if (GetCount(1) <= GetCount(0))
			{
                if (InstantiateNotWin() == -10)                 //возвращает -10, если нет опасности. Возвращает 1, если нашел ход до победы
                    CreateMove(1);
            }
		}

		public void CreateMove(int value)
		{
			if (GetCount(1) <= GetCount(0) && value == 0)
			{
				return;
			}

			List<VectorInt> emptyCellsList = new List<VectorInt>();
			for (int i = 0; i < BoardSize; i++)
			{
				for (int j = 0; j < BoardSize; j++)
				{
					switch (board[i, j])
					{
						case -10:
							emptyCellsList.Add(new VectorInt(){X = i, Y = j});
							break;
					}
				}
			}
			if (emptyCellsList.Count > 0)
			{
				System.Random r = new System.Random();
				int randomIndex = r.Next(0, emptyCellsList.Count);

				InstantiateCell(emptyCellsList[randomIndex].X, emptyCellsList[randomIndex].Y, value);
			}
		}

		private void InstantiateCell(int x, int y, int value)
		{
			switch (value)
			{
				case 0:
					boardGameObject[x, y] = Instantiate(oPrefab, new Vector3(x, 0.0f, y), Quaternion.identity);
					break;
				case 1:
					boardGameObject[x, y] = Instantiate(xPrefab, new Vector3(x, 0.0f, y), Quaternion.identity);
					break;
			}

			board[x, y] = value;
		}

		public void Update()
		{
			ShowText();

			if (IsGameEnded() == 0 || IsGameEnded() == 1 || IsGameEnded() == 2)
			{
				return;
			}

			if (currentInterval < updateInterval)
			{
				currentInterval += Time.deltaTime;
				return;
			}
			else
			{
				currentInterval = 0.0f;
			}
			WaitMove();
			Show();
            InstantiateCubeOnMouse();
        }

		private void ShowText()
		{
			if (IsGameEnded() == 0)
			{
				uiText.text = "Нолики победили";
			}
			if (IsGameEnded() == 1)
			{
				uiText.text = "Крестики победили";
			}
			if (IsGameEnded() == -1)
			{
				uiText.text = "";
			}
			if (IsGameEnded() == 2)
			{
				uiText.text = "Ничья";
			}
		}

		private void RandomCreateBoard()
		{
			System.Random r = new System.Random();
			for (int i = 0; i < BoardSize; i++)
			{
				for (int j = 0; j < BoardSize; j++)
				{

					board[i, j] = r.Next(0, 2);
				}
			}
		}
		private void Show()
		{
			for (int i = 0; i < BoardSize; i++)
			{
				for (int j = 0; j < BoardSize; j++)
				{
					if (boardGameObject[i, j] != null)
					{
						Destroy(boardGameObject[i, j]);
					}
					InstantiateCell(i, j, board[i,j]);
				}
			}
		}
	
		private int IsGameEnded()
		{
			for (int i = 0; i < BoardSize; i++) //три в ряд по Х
			{
				iterator = 0;
				currentState = board[i, 0];
				for (int j = 1; j < BoardSize; j++)
				{
					if (currentState != board[i, j])
					{
					}
					else
					{
						iterator++;
					}
				}
				if (iterator == BoardSize - 1)
				{
					return currentState;
				}
			}

			for (int j = 0; j < BoardSize; j++) //три в ряд по У
			{
				iterator = 0;
				currentState = board[0, j];
				for (int i = 1; i < BoardSize; i++)
				{
					if (currentState != board[i, j])
					{
					}
					else
					{
						iterator++;
					}
				}
				if (iterator == BoardSize - 1)
				{
					return currentState;
				}
			}

			iterator = 0;
			currentState = board[0, 0];
			for (int i = 1; i < BoardSize; i++) //одна диагональ
			{
				if (currentState != board[i, i])
				{
				}
				else
				{
					iterator++;
				}
			}
			if (iterator == BoardSize - 1)
			{
				return currentState;
			}

			iterator = 0;
			currentState = board[0, BoardSize-1];
			for (int i = 1; i < BoardSize; i++) //другая диагональ
			{
				if (currentState != board[i, BoardSize - 1 - i])
				{
				}
				else
				{
					iterator++;
				}
			}
			if (iterator == BoardSize - 1)
			{
				return currentState;
			}
			for (int i = 0; i < BoardSize; i++) //  игра не завершилась
			{
				for (int j= 0; j < BoardSize; j++)
				{
					if(board[i,j] == -10)
					{
						return -1;
					}
				}
			}


			return 2; //ничья
		}
        private void InstantiateCubeOnMouse()               //ф-ция инициализирует oPrefab в нажатом месте. Если место занято, то выходим из функции
        {
            x_oPrefab = clickCube.getX();                   //координата X нажатого куба 
            z_oPrefab = clickCube.getZ();                   //координата Z нажатого куба
            if (board[x_oPrefab, z_oPrefab] == -10)         //если нажали на пустое место, 
                InstantiateCell(x_oPrefab, z_oPrefab, 0);   //то инициализируем oPrefab в нажатом месте
            else
                return;
        }
        private int InstantiateNotWin()                     //ф-ция ставит xPrefab там, где опасное положение 
        {
            int row1 = 0;                                   //сумма 1 строки
            int row2 = 0;                                   //сумма 2 строки
            int row3 = 0;                                   //сумма 3 строки
            int column1 = 0;                                //сумма 1 столбца
            int column2 = 0;                                //сумма 2 столбца
            int column3 = 0;                                //сумма 3 столбца
            int diag1 = 0;                                  //сумма 1 диагонали
            for (int i = 0; i < BoardSize; i++)             //считываем сумму строки и столбцов
            {
                row1 += board[i, 0];
                row2 += board[i, 1];
                row3 += board[i, 2];
                column1 += board[0, i];
                column2 += board[1, i];
                column3 += board[2, i];
                for (int j = 0; j < BoardSize; j++)
                {
                    if(i  == j)
                    {
                        diag1 += board[i, j];
                    }
                }
            }
            for (int i = 0; i < BoardSize; i++)
            {
                if (row1 == -10 && board[i, 0] == -10)          //если в 1 строке сумма значений = - 10 и есть свободное место
                {
                    InstantiateCell(i, 0, 1);                   //то заполняем это свободное место xPrefab'ом
                    Debug.Log("1 строка");
                    return 1;
                }
                if (row2 == -10 && board[i, 1] == -10)          //если во 2 строке сумма значений = - 10 и есть свободное место
                {
                    InstantiateCell(i, 1, 1);                   //то заполняем это свободное место xPrefab'ом
                    Debug.Log("2 строка");
                    return 1;
                }
                if (row3 == -10 && board[i, 2] == -10)          //если в 3 строке сумма значений = - 10 и есть свободное место          
                {
                    InstantiateCell(i, 2, 1);                   //то заполняем это свободное место xPrefab'ом
                    Debug.Log("3 строка");
                    return 1;
                }
                if (column1 == -10 && board[0, i] == -10)       //если в 1 столбце сумма значений = - 10 и есть свободное место
                {
                    InstantiateCell(0, i, 1);                   //то заполняем это свободное место xPrefab'ом
                    Debug.Log("1 столбец");
                    return 1;
                }
                if (column2 == -10 && board[1, i] == -10)       //если во 2 столбце сумма значений = - 10 и есть свободное место
                {
                    InstantiateCell(1, i, 1);                   //то заполняем это свободное место xPrefab'ом
                    Debug.Log("2 столбец");
                    return 1;
                }
                if (column3 == -10 && board[2, i] == -10)       //если в 3 столбце сумма значений = - 10 и есть свободное место
                {
                    InstantiateCell(2, i, 1);                   //то заполняем это свободное место xPrefab'ом
                    Debug.Log("3 столбец");
                    return 1;
                }
                for(int j = 0; j < BoardSize; j++)
                {
                    if (i == j)
                    {
                        if (diag1 == -10 && board[i, j] == -10) //если в 1 дианонили сумма значений = - 10 и есть свободное место
                        {
                            InstantiateCell(i, j, 1);           //то заполняем это свободное место xPrefab'ом
                            Debug.Log("1 диаганаль");
                            return 1;
                        }
                    }
                }
            }
            return -10;
        }
    }
}