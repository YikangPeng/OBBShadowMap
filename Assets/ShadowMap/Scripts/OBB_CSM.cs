using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OBB_CSM : MonoBehaviour
{
    public GameObject BoundingBox;
    public Transform Light;

    private Camera shadowCamera;

    private Vector3[] pointWorldspace = new Vector3[8];
    private Vector3[] pointLightspace = new Vector3[8];
    private float[] xLightObb = new float[8];
    private float[] yLightObb = new float[8];
    private float[] zLightObb = new float[8];

    // Start is called before the first frame update
    void Start()
    {
        shadowCamera = GetComponent<Camera>();
        shadowCamera.SetReplacementShader(Shader.Find("Custom/Depthmap"), "RenderType");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LateUpdate()
    {
        
        //偷个懒直接用一个box的八个顶点
        //转到灯光坐标系        

        pointWorldspace = GetColliderVertexPositions(BoundingBox);

        for (int i = 0; i < pointWorldspace.Length; i++)
        {
            pointLightspace[i] = Light.InverseTransformPoint(pointWorldspace[i]);
        }

        float averageX = 0.0f;
        float averageY = 0.0f;

        //构造XY的协方差矩阵
        for (int i = 0; i < pointLightspace.Length; i++)
        {
            averageX += pointLightspace[i].x;
            averageY += pointLightspace[i].y;
        }

        averageX = averageX / pointLightspace.Length;
        averageY = averageY / pointLightspace.Length;

        float CovXX = 0.0f;
        float CovXY = 0.0f;
        float CovYY = 0.0f;

        for (int i = 0; i < pointLightspace.Length; i++)
        {
            CovXX = CovXX + (pointLightspace[i].x - averageX) * (pointLightspace[i].x - averageX);
            CovXY = CovXY + (pointLightspace[i].x - averageX) * (pointLightspace[i].y - averageY);
            CovYY = CovYY + (pointLightspace[i].y - averageY) * (pointLightspace[i].y - averageY);
        }

        CovXX = CovXX / pointLightspace.Length;
        CovXY = CovXY / pointLightspace.Length;
        CovYY = CovYY / pointLightspace.Length;

        //计算协方差矩阵的较大的特征根        
        float Root = (CovXX + CovYY + Mathf.Sqrt((CovXX + CovYY) * (CovXX + CovYY) - 4 * (CovXX * CovYY - CovXY * CovXY))) / 2;

        //特征根求特征向量  u@axis = set(1 , (f@r - f@xx) / f@xy);
        //Vector2 Axis = new Vector2(1.0f, ((Root - CovXX) / CovXY));
        Vector3 Axis = new Vector3(1.0f, (Root - CovXX) / CovXY , 0.0f);
        Axis = Axis.normalized;

        Debug.Log(Axis + "+" + Light.TransformDirection(Axis));

        Vector3 xAxis = Vector3.Cross(Vector3.forward, Axis).normalized;

        //计算obb中心点
        for (int i = 0; i < pointLightspace.Length; i++)
        {
            
            Vector3 xyplane = pointLightspace[i];            
            xyplane.z = 0.0f;

            xLightObb[i] = Vector3.Dot(xAxis, xyplane);
            yLightObb[i] = Vector3.Dot(Axis, xyplane);
            zLightObb[i] = pointLightspace[i].z;
        }

        float minX = Mathf.Min(xLightObb);
        float maxX = Mathf.Max(xLightObb);

        float minY = Mathf.Min(yLightObb);
        float maxY = Mathf.Max(yLightObb);

        float minZ = Mathf.Min(zLightObb);
        float maxZ = Mathf.Max(zLightObb);

        Vector3 center = ((minX + maxX) / 2) * xAxis + ((minY + maxY) / 2) * Axis;
        center.z = minZ - 1.0f;

        float size = Mathf.Max(Mathf.Abs((minX - maxX) / 2) , Mathf.Abs((minY - maxY) / 2));

        //设置相机位置、旋转
        transform.rotation = Quaternion.LookRotation(Light.forward, Light.TransformDirection(Axis));
        transform.position = Light.TransformPoint(center);


        shadowCamera.orthographicSize = size;

        shadowCamera.RenderWithShader(Shader.Find("Custom/Depthmap"), "RenderType");
        Shader.SetGlobalTexture("_LightDepth", shadowCamera.targetTexture);
        Shader.SetGlobalMatrix("_CM", shadowCamera.projectionMatrix * shadowCamera.worldToCameraMatrix);

    }


    //偷个懒直接用一个box的八个顶点
    public Vector3[] GetColliderVertexPositions(GameObject obj)
    {
        BoxCollider b = obj.GetComponent<BoxCollider>(); //retrieves the Box Collider of the GameObject called obj
        Vector3[] vertices = new Vector3[8];
        vertices[0] = obj.transform.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y, -b.size.z) * 0.5f);
        vertices[1] = obj.transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, -b.size.z) * 0.5f);
        vertices[2] = obj.transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, b.size.z) * 0.5f);
        vertices[3] = obj.transform.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y, b.size.z) * 0.5f);
        vertices[4] = obj.transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, -b.size.z) * 0.5f);
        vertices[5] = obj.transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, -b.size.z) * 0.5f);
        vertices[6] = obj.transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, b.size.z) * 0.5f);
        vertices[7] = obj.transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, b.size.z) * 0.5f);

        return vertices;
    }
}
