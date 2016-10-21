using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour {

    public GameObject flowerPrefab;

    public GameObject enemyPrefab;

    public Vector3 spawnRangeMax;

    public Vector3 spawnRangeMin;

    public Vector3 flowerSpawnPos, enemySpawnPos;

    private Text scoreText;

    [Range(0,100)]
    public float spawnFrequency;

    public float spawnWaitTime;

    private float sWTime;

    public float flowerAmountToSpawn, enemyAmountToSpawn;

    public bool shouldSpawnFlower,shouldSpawnEnemy;

    private float flowerXPos,enemyXPos;

    private float flowerZPos,enemyZPos;

    public int gameScore;

    void Start()
    {
        scoreText = FindObjectOfType<Text>();
    }
	void Update ()
    {

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
        scoreText.text = "Revenges: " + gameScore.ToString();
	}
  
}
