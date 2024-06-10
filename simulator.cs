using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class Simulator : MonoBehaviour
{
   [SerializeField] float width;
   [SerializeField] float height;
   [SerializeField] double radius;
   [SerializeField] int numParticles;
   [SerializeField] float pressureConstant;
   [SerializeField] float grav;
   [SerializeField] float secondPressureConstant;


   float [,] Warr;
   float [,] prevWarr;


   int rows;
   int cols;


   public Camera camera;


   public GameObject ball;
   public List<GameObject> particles;
   public List<GameObject> oldParticles;
   public List<int>[,] grid;
   public List<(float, float)> delta;
   public List<(int, int)> directions;
   public List<(float, float)> speed;
   public List<(float, float)> acceleration;
   public List<float> density;
   public List<float> pressure;


   public float kernel(int idx1, int idx2){
       GameObject one = particles[idx1];
       GameObject two = particles[idx2];
       float distance = Vector2.Distance(one.transform.position, two.transform.position);


       float q = (float) (distance / radius);
       double W;


       if (q >= 0f & q <= 0.5f){
           W=6*Math.Pow(q,3) - 6*Math.Pow(q,2)+1;
       }
       else if(q > 0.5 & q <= 1){
           W=2*Math.Pow((1-q),3);
       }
       else{
           W=0;
       }
       return (float)(W/(Math.Pow(radius,2)));
   }


   Vector2 calcPressure (int currIdx, int neighborIdx){
       if(Math.Pow(pressure[currIdx],2)==0){
           // Debug.Log("Pressure curr is 0: "+pressure[currIdx]);
           // pressure[currIdx] = 0.01f;
           return new Vector2(0,0);
       }
       else{
           // Debug.Log("Pressure curr is NOT 0: "+pressure[currIdx]);
       }
       if(Math.Pow(pressure[neighborIdx],2)==0){
           // Debug.Log("Pressure neighbor is 0: "+pressure[neighborIdx]);
           // pressure[neighborIdx] = 0.01f;
           float local_pressure_thing = (float)((density[currIdx] / Math.Pow(pressure[currIdx],2)));
           int local_j = Math.Max(currIdx, neighborIdx);
           int local_k = Math.Min(currIdx, neighborIdx);
           float local_dx = delta[currIdx].Item1;
           float local_dy = delta[currIdx].Item2;
           if(local_dx == 0){
               // Debug.Log("dx is 0");
               local_dx = 0.0001f;
           }
           if(local_dy == 0){
               // Debug.Log("dy is 0");
               local_dy = 0.0001f;
           }
           float local_a_x = (float)(local_pressure_thing /local_dx * (Warr[local_j,local_k] - prevWarr[local_j,local_k]));
           float local_a_y = (float)(local_pressure_thing /local_dy * (Warr[local_j,local_k] - prevWarr[local_j,local_k]));
           return new Vector2(-local_a_x,-local_a_y);
       }
       else{
           // Debug.Log("Pressure neighbor is NOT 0: "+pressure[neighborIdx]);
       }
       float pressure_thing = (float)((density[currIdx] / Math.Pow(pressure[currIdx],2)) + (density[neighborIdx] / Math.Pow(pressure[neighborIdx],2)));
       int j = Math.Max(currIdx, neighborIdx);
       int k = Math.Min(currIdx, neighborIdx);
       float dx = delta[currIdx].Item1;
       float dy = delta[currIdx].Item2;
       if(dx == 0){
           // Debug.Log("dx is 0");
           dx = 0.0001f;
       }
       if(dy == 0){
           // Debug.Log("dy is 0");
           dy = 0.0001f;
       }
       float a_x = (float)(pressure_thing /dx * (Warr[j,k] - prevWarr[j,k]));
       float a_y = (float)(pressure_thing /dy * (Warr[j,k] - prevWarr[j,k]));
       // Debug.Log(a_x + " " + a_y);
       return new Vector2(-a_x,-a_y);
   }


   void updateOldStuff(){
       for(int i=0;i<numParticles;i++){
           for(int j=0;j<numParticles;j++){
               prevWarr[i,j] = Warr[i,j];
           }
       }


   }


   void updatePhysics(){
       for(int i=0;i<rows;i++){
           for(int j=0;j<cols;j++){
               for(int k=0; k<grid[i,j].Count;k++){
                       int[,] neighborGrids = {
                           { -1, -1 }, { -1, 0 }, { -1, 1 },
                           { 0, -1 }, { 0, 0 }, { 0, 1 },
                           { 1, -1 }, { 1, 0 }, { 1, 1 }
                       };
                   int currIdx = grid[i,j][k];
                   density[currIdx] = 0;
                   for (int z = 0; z < 9; z++){
                           int[] gridIndices = { i+ neighborGrids[z,0], j +  neighborGrids[z,1]};
                            // Check if grid position is within bounds
                           if (gridIndices[0] >= 0 && gridIndices[0] < rows && gridIndices[1] >= 0 && gridIndices[1] < cols)
                           {
                               for(int l=0; l<grid[gridIndices[0], gridIndices[1]].Count; l++){
                                   int neighborIdx = grid[gridIndices[0], gridIndices[1]][l];
                                   if(Vector2.Distance(particles[currIdx].transform.position, particles[neighborIdx].transform.position)<= radius){
                                       int x = Math.Max(currIdx, neighborIdx);
                                       int y = Math.Min(currIdx, neighborIdx);
                                       Warr[x,y] = kernel(currIdx, neighborIdx);
                                       density[currIdx] += Warr[x,y];
                                   }
                                  
                               }
                           }
                       }
                   pressure[currIdx] = (float)(pressureConstant*(Math.Pow(density[currIdx]/secondPressureConstant, 7)-1));
                   Debug.Log("Pressure "+pressure[currIdx]);
               }
           }
       }
       for(int i=0;i<rows;i++){
           for(int j=0;j<cols;j++){
               for(int k=0;k<grid[i,j].Count;k++){
                   int currIdx = grid[i,j][k];
                   Vector2 curr_pressure = new Vector2(0, 0);
                   Vector2 curr_viscosity = new Vector2(0, 0);
                   Vector2 curr_density = new Vector2(0, 0);
                   Vector2 other = new Vector2(0, grav);
                   int[,] neighborGrids = {
                       { -1, -1 }, { -1, 0 }, { -1, 1 },
                       { 0, -1 }, { 0, 0 }, { 0, 1 },
                       { 1, -1 }, { 1, 0 }, { 1, 1 }
                   };
                   for(int z=0; z<9;z++){
                       int[] gridIndices = { i+ neighborGrids[z,0], j +  neighborGrids[z,1]};
                       if (gridIndices[0] >= 0 && gridIndices[0] < rows && gridIndices[1] >= 0 && gridIndices[1] < cols)
                       {
                           for(int l=0; l<grid[gridIndices[0],gridIndices[1]].Count;l++){
                               int neighborIdx=grid[gridIndices[0],gridIndices[1]][l];
                               if(Vector2.Distance(particles[currIdx].transform.position, particles[neighborIdx].transform.position)<= radius){
                                   curr_pressure += calcPressure(currIdx, neighborIdx);
                               }
                           }
                       }
                   }


                   acceleration[currIdx] = (Math.Sign((curr_pressure + other).x)*(float)Math.Pow(Math.Abs((curr_pressure + other).x),0.5), Math.Sign((curr_pressure + other).y)*(float)Math.Pow(Math.Abs((curr_pressure+other).y), 0.5));
                   Debug.Log("Acceleration: "+acceleration[currIdx]);
               }
           }
       }
   }


   void updateParticles(){
       for(int i=0;i<particles.Count;i++){
           oldParticles[i] = particles[i];
       }
       for(int i=0; i<particles.Count; i++){
           speed[i] = (speed[i].Item1+Time.deltaTime*acceleration[i].Item1, speed[i].Item2+Time.deltaTime*acceleration[i].Item2);
           particles[i].transform.position = new Vector2(particles[i].transform.position.x + Time.deltaTime*directions[i].Item1*speed[i].Item1,
           particles[i].transform.position.y + Time.deltaTime*directions[i].Item2*speed[i].Item2);
           delta[i] = (particles[i].transform.position.x - oldParticles[i].transform.position.x, particles[i].transform.position.y - oldParticles[i].transform.position.y);
       }
   }
   void updateGrid(){
       for(int i=0;i<rows;i++){
           for(int j=0;j<cols;j++){
               grid[i,j].Clear();
           }
       }
       for(int k=0;k<particles.Count;k++){
           Transform transform = particles[k].GetComponent<Transform>();


           int i = Mathf.FloorToInt((float)(transform.position.x/radius));
           int j = Mathf.FloorToInt((float)(transform.position.y/radius));


           if(transform.position.x > height || transform.position.y > width || transform.position.x<0 || transform.position.y<0){
               if(transform.position.x > height){
                   particles[k].transform.position = new Vector2(2*height - particles[k].transform.position.x, particles[k].transform.position.y);
                   speed[k] = (speed[k].Item1*-0.5f, speed[k].Item2*0.5f);
               }
               if(transform.position.x<0){
                   particles[k].transform.position = new Vector2(-particles[k].transform.position.x, particles[k].transform.position.y);
                   speed[k] = (speed[k].Item1*-0.5f, speed[k].Item2*0.5f);
               }
               if(transform.position.y > width){
                   particles[k].transform.position = new Vector2(particles[k].transform.position.x, 2*width - particles[k].transform.position.y);
                   speed[k] = (speed[k].Item1*0.5f, speed[k].Item2*-0.5f);
               }
               if(transform.position.y<0){
                   particles[k].transform.position = new Vector2(particles[k].transform.position.x, -particles[k].transform.position.y);
                   speed[k] = (speed[k].Item1*0.5f, speed[k].Item2*-0.5f);
               }
           }
           if(i>=rows || i<0 || j>=cols || j<0){
               continue;
           }
           grid[i,j].Add(k);
       }
   }


   Bounds GetCameraBounds(Camera camera)
   {
       float cameraHeight = 2f * camera.orthographicSize;
       float cameraWidth = cameraHeight * camera.aspect;


       Vector3 cameraPosition = camera.transform.position;


       float minX = cameraPosition.x - cameraWidth / 2f;
       float maxX = cameraPosition.x + cameraWidth / 2f;
       float minY = cameraPosition.y - cameraHeight / 2f;
       float maxY = cameraPosition.y + cameraHeight / 2f;


       Bounds bounds = new Bounds(new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f), new Vector3(cameraWidth, cameraHeight, 0f));
       return bounds;
   }


   void Start()
   {
       Warr = new float[numParticles, numParticles];
       prevWarr = new float[numParticles, numParticles];
       for(int i=0;i<numParticles;i++){
           for(int j=0;j<numParticles;j++){
               Warr[i,j] = 0;
               prevWarr[i,j]=0;
           }
       }
       camera = Camera.main;


       float cameraHeight = camera.orthographicSize;
       float cameraWidth = cameraHeight * camera.aspect;
       Vector3 newCameraPosition = new Vector3(cameraWidth, cameraHeight, camera.transform.position.z);


       // Set the camera's position
       camera.transform.position = newCameraPosition;

       Bounds cameraBounds = GetCameraBounds(camera);

       height = cameraBounds.max.x;
       width = cameraBounds.max.y;


       cols = (int) Math.Ceiling(width/radius);
       rows = (int) Math.Ceiling(height/radius);
       grid = new List<int>[rows,cols];
       for(int i=0;i<rows;i++){
           for(int j=0;j<cols;j++){
               grid[i,j]= new List<int>();
           }
       }       
       particles = new List<GameObject>();
       directions = new List<(int, int)>();
       speed = new List<(float, float)>();
       acceleration = new List<(float, float)>();
       delta = new List<(float, float)>();
       oldParticles = new List<GameObject>();
       density = new List<float>();
       pressure = new List<float>();
       System.Random random = new System.Random();
       for(int i=0; i<numParticles;i++){
           double x = random.NextDouble() * width;
           double y = random.NextDouble() * height;


           Vector2 pos = new Vector2((float)x, (float)y);
           Vector2 newPos = new Vector2((float)(x+0.1), (float)(y+0.1));
           GameObject b = (GameObject) Instantiate(ball, pos, transform.rotation);
           particles.Add(b);
           oldParticles.Add(b);
           delta.Add((0.1f,0.1f));
           directions.Add((1,1));
           speed.Add((1,1));
           acceleration.Add((0,0));
           density.Add(1);
           pressure.Add(1);
       }
       updateGrid();
   }


   // Update is called once per frame
   void Update()
   {
       string str = "";
       for(int i=0; i<numParticles;i++){
           for(int j=0;j<numParticles;j++){
              str += Warr[i,j]+" ";
           }
           str+='\n';
       }
       Debug.Log(str);
       updateOldStuff();
       updatePhysics();
       updateParticles();
       updateGrid();

   }
}



