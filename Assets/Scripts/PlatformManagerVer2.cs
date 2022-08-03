using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PlatformManagerVer2 : PlatformGenericSinglton<PlatformManagerVer2>
{

        #region Platform Manager Custom Events
        public delegate void PlatformManagerChanged(PlatformConfigurationData data);
        public static event PlatformManagerChanged OnPlatformManagerChanged;

        public delegate void PlatformManagerUpdateUI();
        public static event PlatformManagerUpdateUI OnPlatformManagerUpdateUI;
        #endregion

        public enum ColorShade
        {
            GrayScale,
            RedScale,
            GreenScale,
            BlueScale,
            Random
        }
        float speed = 0.01f;
        public float[,] currPos;
        public float[,] nextPos;
        public GameObject PlatformBasePref;
        public int oldM;
        public int oldN;

        public PlatformConfigurationData configurationData = new PlatformConfigurationData();
        float spaceX = 0.0f;
        float spaceZ = 0.0f;
        float ySpace = 0.0f;
        public GameObject[,] platformNode;

        public PlatformDataNodeVer2[,] platformProgram;

        public bool SimulateTest = false;
        public bool Program = false;

        //public ColorShade shade = ColorShade.GrayScale;

        #region Selected Node Information Display Variables
        [Header("Selected Node UI Controls")]
        GameObject currentSelection = null;
        //public Text txtSelectedNodeName;
        //public Text txtSelectedNodePosition;
        //public Image imgSelectedNodeColor;
        #endregion

        private void OnEnable()
        {
            UIManager.BuildPlatformOnClicked += UIManager_BuildPlatformOnClicked;
            UIManager.OnWriteProgramData += UIManager_OnWriteProgramData;
            //UIManager.OnUpdatePlatformNode += UIManager_OnUpdatePlatformNode;
            PlatformDataNodeVer2.OnNodeSimulationComplete += PlatformDataNodeVer2_OnNodeSimulationComplete;

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void OnDisable()
        {
            UIManager.BuildPlatformOnClicked -= UIManager_BuildPlatformOnClicked;
            UIManager.OnWriteProgramData -= UIManager_OnWriteProgramData;
            //UIManager.OnUpdatePlatformNode -= UIManager_OnUpdatePlatformNode;
            PlatformDataNodeVer2.OnNodeSimulationComplete -= PlatformDataNodeVer2_OnNodeSimulationComplete;
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
        }

        private void UIManager_BuildPlatformOnClicked(PlatformConfigurationData pcd)
        {
            configurationData = pcd;

            BuildPlatform();

            oldM = pcd.M;
            oldN = pcd.N;
            /*for(int i = 0; i < oldM; i++){
                for(int j = 0; j < oldN; j++){
                    nextPos[i, j] = 0.0f;
                }
            }*/
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if(SceneManager.GetActiveScene().name.Contains("Simulate")){
                SimulateTest = true;
                Program = false;
            } else{
                SimulateTest = false;
            }
            if(SimulateTest){
                DestroyPlatform();
                BuildPlatformFromFile();
                if(OnPlatformManagerUpdateUI != null)
                    OnPlatformManagerUpdateUI();
            }else{
                if(platformNode != null){
                    if(SceneManager.GetActiveScene().name.Contains("Program")){
                        Program = true;
                        SimulateTest = false;
                    } else {
                        Program = false;
                    }
                    BuildPlatform();

                    if(OnPlatformManagerUpdateUI != null)
                        OnPlatformManagerUpdateUI();
                }
                if(SceneManager.GetActiveScene().name == "Main Menu")
                    DestroyPlatform();
                Debug.Log(SceneManager.GetActiveScene().name);
            }
        }

        //private void UIManager_OnUpdatePlatformNode(PlatformDataNodeVer2 data)
        //{
        //    PlatformDataNodeVer2 pdn = platformNode[data.i, data.j].GetComponent<PlatformDataNodeVer2>();
        //    pdn = data;
        //}
        private void PlatformDataNodeVer2_OnNodeSimulationComplete(PlatformDataNodeVer2 data){
            if(platformProgram == null)
                return;
            Debug.Log(String.Format("Data Node [{0} , {1}] Completeed sim.", data.i, data.j));
            PlatformDataNodeVer2 d1 = platformProgram[data.i, data.j];
            d1.NextPosition = 0;
            platformProgram[data.i, data.j] = d1;

            if(data.i + 1 >= configurationData.M){
                PlatformDataNodeVer2 d2a = platformProgram[0, data.j];
                d2a.NextPosition = data.NextPosition;
                platformProgram[0, data.j] = d2a;
            }else{
                PlatformDataNodeVer2 d2b = platformProgram[data.i+1, data.j];
                d2b.NextPosition = data.NextPosition;
                platformProgram[data.i+1, data.j] = d2b;
            }
            for(int j = 0; j < configurationData.N; j++){
               platformNode[data.i,j].GetComponent<PlatformDataNodeVer2>().Shift();
            }
        }

        private void UIManager_OnWriteProgramData()
        {
            // we will save the platform configuration data 
            // we will save the platform node program data
            Debug.Log("SAVING PLATFORM PROGRAM DATA ... SIMULATION");
            //Debug.Log(configurationData.ToString());

            using (StreamWriter outputFile = new StreamWriter(Path.Combine(Application.dataPath, "WriteLines.txt")))
            {
                outputFile.WriteLine(configurationData.ToString());
                for (int i = 0; i < oldM; i++)
                {
                    for (int j = 0; j < oldN; j++)
                    {
                        //Debug.Log(platformNode[i, j].GetComponent<PlatformDataNodeVer2>().ToString());
                        outputFile.WriteLine(platformNode[i, j].GetComponent<PlatformDataNodeVer2>().ToString());
                    }
                }
            }
        }
        // Use this for initialization
        void Start()
        {
        }

        #region BUILD PLATFORM FROM UI

        public void DestroyPlatform()
        {
            // check to see if there is no platform currently configured
            // if there is one, delete it
            if (platformNode != null)
            {
                for (int i = 0; i < oldM; i++)
                {
                    for (int j = 0; j < oldN; j++)
                    {
                        Destroy(platformNode[i, j], 0.1f);
                    }
                }

                platformNode = null;
            }
        }

        public void BuildPlatform()
        {
            DestroyPlatform();
            platformNode = new GameObject[configurationData.M, configurationData.N];

            spaceX = 0;
            spaceZ = 0;

            for (int i = 0; i < configurationData.M; i++)
            {
                spaceZ = 0.0f;
                for (int j = 0; j < configurationData.N; j++)
                {
                    float x = (i * 1) + spaceX;
                    float z = (j * 1) + spaceZ;

                    //Debug.Log(string.Format("x={0} z={1}", x, z));
                    // create a platform pref ...
                    var platformBase = Instantiate(PlatformBasePref,
                                                   new Vector3(x, ySpace, z),
                                                   Quaternion.identity);

                    platformBase.name = string.Format("Node[{0},{1}]", i, j);

                    platformBase.AddComponent<PlatformDataNodeVer2>();

                    platformNode[i, j] = platformBase;

                    PlatformDataNodeVer2 pdn = platformBase.transform.GetComponent<PlatformDataNodeVer2>();
                    pdn.Program = Program;
                    pdn.i = i;
                    pdn.j = j;
                    spaceZ += configurationData.deltaSpace;
                }
                spaceX += configurationData.deltaSpace;
            }

            if (OnPlatformManagerChanged != null)
                OnPlatformManagerChanged(configurationData);
        }

        public void StartSimulationButtonClick()
        {
            SimulateTest = !SimulateTest;
        }
        #endregion

        public static bool NearlyEquals(float? value1, float? value2, float unimportantDifference = 0.01f)
        {
            if (value1 != value2)
            {
                if (value1 == null || value2 == null)
                    return false;

                return Math.Abs(value1.Value - value2.Value) < unimportantDifference;
            }

            return true;
        }
        public void NextPositionReachedForAllNodesInRow(int rowId){
            if(platformProgram == null)
                return;
            bool result = true;

            for(int j = 0; j < configurationData.N; j++){
                var p = platformNode[rowId, j].GetComponent<PlatformDataNodeVer2>();
                if(platformProgram[rowId, j].NextPosition > 0 && !p.NextPositionReached){
                    result = false;
                }
            }
            if(result){
                for(int j = 0; j < configurationData.N; j++){
                    platformNode[rowId, j].GetComponent<PlatformDataNodeVer2>().Shift();
                }
            }
        }
        void BuildPlatformFromFile(){
            Debug.Log("BUILDING FROM FILE ");
            using(System.IO.StreamReader sr = new System.IO.StreamReader(Path.Combine(Application.dataPath, "WriteLines.txt"))){
                String line;

                line = sr.ReadLine();
                string[] configData = line.Split(',');
                PlatformConfigurationData pcd = new PlatformConfigurationData();

                pcd.M = Convert.ToInt32(configData[0]);
                pcd.N = Convert.ToInt32(configData[1]);
                pcd.deltaSpace = (float)Convert.ToDouble(configData[2]);
                pcd.RandomHeight = (float)Convert.ToDouble(configData[3]);
                Debug.Log(pcd.ToString());

                configurationData = pcd;
                BuildPlatform();
                platformProgram = new PlatformDataNodeVer2[configurationData.M, configurationData.N];

                string[] data;
                while((line = sr.ReadLine()) != null){
                    //Debug.Log(line);
                    data = line.Split(',');
                    PlatformDataNodeVer2 nodeData = new PlatformDataNodeVer2();
                    nodeData.i = Convert.ToInt32(data[0]);
                    nodeData.j = Convert.ToInt32(data[1]);
                    nodeData.NextPosition = (float)Convert.ToDouble(data[2]);

                    platformProgram[nodeData.i, nodeData.j] = nodeData;
                    //Debug.Log("Next Position: " + platformProgram[nodeData.i, nodeData.j].NextPosition);
                }
            }
            /* != null)
                OnPlatformManagerChanged(configurationData);*/
        }
        int moveIndex = 0;
        bool ProgramNodeDetected;
        bool MakeMove = false;
        // Update is called once per frame
        private void  Update()
        {

            // check to see if platform has been build
            if (platformNode == null)
                return;

            if (Input.GetKey(KeyCode.Q))
                Application.Quit();
            if(SimulateTest){
                if(!MakeMove){
                    MakeMove = true;
                    for(int i = 0; i < configurationData.M; i++){
                        for(int j = 0; j < configurationData.N; j++){
                            if(platformProgram[i,j].NextPosition > 0){
                                //Debug.Log("Inside loop");
                                PlatformDataNodeVer2 tmp = platformProgram[i,j];
                                //Vector3 currPosVec = new Vector3(platformNode[i,j].transform.position.x, platformProgram[i,j].NextPosition ,platformNode[i,j].transform.position.z);
                                //Vector3 nextPosVec = new Vector3(platformNode[i,j].transform.position.x, 0 ,platformNode[i,j].transform.position.z);
                                /**/
                                platformNode[i,j].GetComponent<PlatformDataNodeVer2>().SimulateNode(tmp);
                                /*if(i < configurationData.M-1 ){
                                    //NextPositionReachedForAllNodesInRow(i);
                                    int nextNode = i+1;
                                    platformProgram[nextNode, j].NextPosition = platformProgram[i,j].NextPosition;
                                    platformNode[i,j].transform.position = Vector3.Lerp(platformNode[i,j].transform.position,
                                                                                new Vector3(platformNode[i,j].transform.position.x,
                                                                                        0,
                                                                                        platformNode[i,j].transform.position.z),
                                                                                        Time.deltaTime);
                                }*/
                                //PlatformDataNodeVer2_OnNodeSimulationComplete(tmp);
                                //simulateGrid(i,j, platformProgram[i,j].NextPosition);
                                /*float time = 0.0f;
                                if(time < 1){
                                    time += Time.deltaTime;
                                    platformNode[i, j].transform.position = Vector3.Lerp(platformNode[i,j].transform.position, currPosVec, time); 
                                }
                                moveIndex = i-1;
                                Debug.Log("Move Index: " + moveIndex);
                                if(moveIndex > 0){
                                    platformProgram[moveIndex, j].NextPosition = platformProgram[i,j].NextPosition;
                                    time = 0.0f;
                                    if(time < 1){
                                        time += Time.deltaTime;
                                        platformNode[i, j].transform.position = Vector3.Lerp(platformNode[i,j].transform.position, nextPosVec, time); 
                                    }
                                }*/
                                /*if(moveIndex == 0){
                                    platformNode[i,j].GetComponent<PlatformDataNodeVer2>().Shift();
                                }*/
                            }
                        }
                        //NextPositionReachedForAllNodesInRow(i);
                    }
                    
                    MakeMove = false;
                }
            }
            #region Object Selection
            // you can select only if in program mode/scene
            if (Program)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    if (IsPointerOverUIObject())
                        return;

                    #region Screen To World
                    RaycastHit hitInfo = new RaycastHit();
                    bool hit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo);
                    if (hit)
                    {
                        #region COLOR
                        if(hitInfo.transform.tag == "Plane"){
                            if (currentSelection != null)
                            {
                                PlatformDataNodeVer2 pdn = currentSelection.transform.GetComponent<PlatformDataNodeVer2>();
                                pdn.ResetDataNode();
                            }

                            currentSelection = hitInfo.transform.gameObject;
                            PlatformDataNodeVer2 newPdn = currentSelection.transform.GetComponent<PlatformDataNodeVer2>();
                            newPdn.SelectNode();
                        }
                        #endregion
                    }
                    else
                    {
                        Debug.Log("No hit");
                    }
                    #endregion
                }
            }
            #endregion
        }
        int MovingIndex = 0;
        /*public void simulateGrid(int i, int j, float k){
            moveIndex = i;
            int reset;
            while(moveIndex >= 0){
                float currPos =  k;
                Vector3 currPosVec = new Vector3(moveIndex, k, j);
                if(NearlyEquals(currPos, 0)){
                    reset = 0;
                }
            }
        }*/
        private void ApplyProgramToPlatform(){
            for(int i = 0; i < configurationData.M; i++){
                for(int j = 0; j < configurationData.N; j++){
                    platformNode[i,j].GetComponent<PlatformDataNodeVer2>().SimulateNode(platformProgram[i,j]);
                }
            }
        }
        void ShiftProgram(PlatformDataNodeVer2[,] array, int shift = 1){
            int r = configurationData.M;
            int c = configurationData.N;
            for(int row = 0; row < r; row++){
                int rowLength = c;

            }
        }
        /// <summary>
        /// Used to determine if we are over UI element or not.
        /// </summary>
        /// <returns></returns>
        private bool IsPointerOverUIObject()
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            foreach (var result in results)
            {
                Debug.Log(result.gameObject.name);
            }
            return results.Count > 0;
        }

}
