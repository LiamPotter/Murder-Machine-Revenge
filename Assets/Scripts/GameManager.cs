using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour {

    private PlayerMovement playerObject;

    public bool startedGame;

    public bool spawnedPreliminary;

    public Image startMenuPanel;

    public Image deathMenuPanel;

    public Transform playerBeginPos;

    public GameObject flowerPrefab;

    public GameObject enemyPrefab;

    public Vector3 spawnRangeMax;

    public Vector3 spawnRangeMin;

    public Vector3 flowerSpawnPos, enemySpawnPos;

    public Text scoreText,healthText;

    [Range(0,10)]
    public float spawnFrequency;

    public float spawnDifficultyIncreaser;

    public float waitTimeForDifficultyIncrease;

    private float sWTime;

    public float flowerAmountToSpawn, enemyAmountToSpawn;

    public bool shouldSpawnFlower,shouldSpawnEnemy;

    private float flowerXPos,enemyXPos;

    private float flowerZPos,enemyZPos;

    public int gameScore;

    void Start()
    {
        //scoreText = FindObjectOfType<Text>();
        startedGame = false;
        spawnedPreliminary = true;
        playerObject = FindObjectOfType<PlayerMovement>();
        playerObject.transform.position = playerBeginPos.position;
    }

    void Update()
    {
        if (startedGame)
        {
            startMenuPanel.gameObject.SetActive(false);
            if(!spawnedPreliminary)
            {
                InvokeRepeating("TrySpawnEnemy", 0, spawnFrequency);
                InvokeRepeating("TryToSpawnFlower", 0, spawnFrequency);
                InvokeRepeating("IncreaseSpawnRate", waitTimeForDifficultyIncrease, waitTimeForDifficultyIncrease);
                playerObject.canMove = true;
                spawnedPreliminary = true;
            }
        }
        if (flowerAmountToSpawn > 0)
        {
            shouldSpawnFlower = true;
        }
        else shouldSpawnFlower = false;
        if (enemyAmountToSpawn > 0)
        {
            shouldSpawnEnemy = true;
        }
        else shouldSpawnEnemy = false;
	    if(shouldSpawnFlower)
        {
            flowerXPos = Random.Range(spawnRangeMin.x, spawnRangeMax.x);
            flowerZPos = Random.Range(spawnRangeMin.z, spawnRangeMax.z);
            flowerSpawnPos = new Vector3(flowerXPos, flowerPrefab.transform.position.y, flowerZPos);
            GameObject inSceneFlower = Instantiate(flowerPrefab, flowerSpawnPos, Quaternion.identity)as GameObject;
            flowerAmountToSpawn--;
        }
        if(shouldSpawnEnemy)
        {
            enemyXPos = Random.Range(spawnRangeMin.x, spawnRangeMax.x);
            enemyZPos = Random.Range(spawnRangeMin.z, spawnRangeMax.z);
            enemySpawnPos = new Vector3(enemyXPos, enemyPrefab.transform.position.y, enemyZPos);
            GameObject inSceneEnemy = Instantiate(enemyPrefab, enemySpawnPos, Quaternion.identity) as GameObject;
            enemyAmountToSpawn--;
        }
        scoreText.text = "Pixies Murdered: " + gameScore.ToString();
        healthText.text = "HP: " + playerObject.currentHealth;
        if(playerObject.currentHealth<=0)
        {
            ShowDeathScreen();
        }
	}

    public void StartGame()
    {
        startedGame = true;
        spawnedPreliminary = false;
        enemyAmountToSpawn = 2;
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }

    public void ShowDeathScreen()
    {
        deathMenuPanel.gameObject.SetActive(true);
        Time.timeScale = 0;
    }

    void TrySpawnEnemy()
    {
        if (spawnFrequency > 5.1)
            enemyAmountToSpawn = 1;
        if (spawnFrequency>=4&&spawnFrequency<=5)
            enemyAmountToSpawn = 2;
        if (spawnFrequency >= 3 && spawnFrequency <= 3.9)
            enemyAmountToSpawn = 2;
        if (spawnFrequency <= 2.9)
            enemyAmountToSpawn = 2;
    }
    void TryToSpawnFlower()
    {
        if (spawnFrequency > 5.1)
            flowerAmountToSpawn = 2;
        if (spawnFrequency >= 4 && spawnFrequency <= 5)
            flowerAmountToSpawn = 2;
        if (spawnFrequency >= 3 && spawnFrequency <= 3.9)
            flowerAmountToSpawn = 3;
        if (spawnFrequency <= 2.9)
            flowerAmountToSpawn = 3;
    }
    void IncreaseSpawnRate()
    {
        spawnFrequency -= spawnDifficultyIncreaser;
        CancelInvoke("TrySpawnEnemy");
        InvokeRepeating("TrySpawnEnemy", 0, spawnFrequency);
        CancelInvoke("TryToSpawnFlower");
        InvokeRepeating("TryToSpawnFlower", 0, spawnFrequency);
    } 
}
