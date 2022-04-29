using System.Collections.Generic;
using UnityEngine;

public class Wire : MonoBehaviour
{
    private const float thicknessMultiplier = 0.1f;

    public Material simpleMat;
    public Color editCol;

    public Palette palette;

    //public Color
    public Color placedCol;
    public float curveSize = 0.5f;
    public int resolution = 10;
    public float thickness = 1;
    public float selectedThickness = 1.2f;
    [HideInInspector] public Pin startPin;
    [HideInInspector] public Pin endPin;
    private float depth;
    private List<Vector2> drawPoints;
    private float length;

    private LineRenderer lineRenderer;
    private Material mat;
    private bool selected;
    private EdgeCollider2D wireCollider;

    private bool wireConnected;
    public List<Vector2> anchorPoints { get; private set; }

    public Pin ChipInputPin => startPin.pinType == Pin.PinType.ChipInput ? startPin : endPin;

    public Pin ChipOutputPin => startPin.pinType == Pin.PinType.ChipOutput ? startPin : endPin;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        lineRenderer.material = simpleMat;
        mat = lineRenderer.material;
    }

    private void LateUpdate()
    {
        SetWireCol();
        if (wireConnected)
        {
            float depthOffset = 5;

            transform.localPosition = Vector3.forward * (depth + depthOffset);
            UpdateWirePos();
            //transform.position = new Vector3 (transform.position.x, transform.position.y, inputPin.sequentialState * -0.01f);
        }

        lineRenderer.startWidth = (selected ? selectedThickness : thickness) * thicknessMultiplier;
        lineRenderer.endWidth = (selected ? selectedThickness : thickness) * thicknessMultiplier;
    }

    public void SetAnchorPoints(Vector2[] newAnchorPoints)
    {
        anchorPoints = new List<Vector2>(newAnchorPoints);
        UpdateSmoothedLine();
        UpdateCollider();
    }

    public void SetDepth(int numWires)
    {
        depth = numWires * 0.01f;
        transform.localPosition = Vector3.forward * depth;
    }

    private void UpdateWirePos()
    {
        const float maxSqrError = 0.00001f;
        // How far are start and end points from the pins they're connected to (chip has been moved)
        var startPointError = (Vector2) startPin.transform.position - anchorPoints[0];
        var endPointError = (Vector2) endPin.transform.position - anchorPoints[anchorPoints.Count - 1];

        if (startPointError.sqrMagnitude > maxSqrError || endPointError.sqrMagnitude > maxSqrError)
        {
            // If start and end points are both same offset from where they should be, can move all anchor points (entire wire)
            if ((startPointError - endPointError).sqrMagnitude < maxSqrError &&
                startPointError.sqrMagnitude > maxSqrError)
                for (var i = 0; i < anchorPoints.Count; i++)
                    anchorPoints[i] += startPointError;

            anchorPoints[0] = startPin.transform.position;
            anchorPoints[anchorPoints.Count - 1] = endPin.transform.position;
            UpdateSmoothedLine();
            UpdateCollider();
        }
    }

    private void SetWireCol()
    {
        if (wireConnected)
        {
            var onCol = palette.onCol;
            var offCol = palette.offCol;

            // High Z
            if (ChipOutputPin.State == -1)
            {
                onCol = palette.highZCol;
                offCol = palette.highZCol;
            }

            mat.color = ChipOutputPin.State == 0 ? offCol : onCol;
        }
        else
        {
            mat.color = Color.black;
        }
    }

    public void Connect(Pin inputPin, Pin outputPin)
    {
        ConnectToFirstPin(inputPin);
        Place(outputPin);
    }

    public void ConnectToFirstPin(Pin startPin)
    {
        this.startPin = startPin;
        lineRenderer = GetComponent<LineRenderer>();
        mat = simpleMat;
        drawPoints = new List<Vector2>();

        transform.localPosition = new Vector3(0, 0, transform.localPosition.z);

        wireCollider = GetComponent<EdgeCollider2D>();

        anchorPoints = new List<Vector2>();
        anchorPoints.Add(startPin.transform.position);
        anchorPoints.Add(startPin.transform.position);
        UpdateSmoothedLine();
        mat.color = editCol;
    }

    public void ConnectToFirstPinViaWire(Pin startPin, Wire parentWire, Vector2 inputPoint)
    {
        lineRenderer = GetComponent<LineRenderer>();
        mat = simpleMat;
        drawPoints = new List<Vector2>();
        this.startPin = startPin;
        transform.localPosition = new Vector3(0, 0, transform.localPosition.z);

        wireCollider = GetComponent<EdgeCollider2D>();

        anchorPoints = new List<Vector2>();

        // Find point on wire nearest to input point
        var closestPoint = Vector2.zero;
        var smallestDst = float.MaxValue;
        var closestI = 0;
        for (var i = 0; i < parentWire.anchorPoints.Count - 1; i++)
        {
            var a = parentWire.anchorPoints[i];
            var b = parentWire.anchorPoints[i + 1];
            var pointOnWire = MathUtility.ClosestPointOnLineSegment(a, b, inputPoint);
            var sqrDst = (pointOnWire - inputPoint).sqrMagnitude;
            if (sqrDst < smallestDst)
            {
                smallestDst = sqrDst;
                closestPoint = pointOnWire;
                closestI = i;
            }
        }

        for (var i = 0; i <= closestI; i++) anchorPoints.Add(parentWire.anchorPoints[i]);
        anchorPoints.Add(closestPoint);
        if (Input.GetKey(KeyCode.LeftAlt)) anchorPoints.Add(closestPoint);
        anchorPoints.Add(inputPoint);

        UpdateSmoothedLine();
        mat.color = editCol;
    }

    // Connect the input pin to the output pin
    public void Place(Pin endPin)
    {
        this.endPin = endPin;
        anchorPoints[anchorPoints.Count - 1] = endPin.transform.position;
        UpdateSmoothedLine();

        wireConnected = true;
        UpdateCollider();
    }

    // Update position of wire end point (for when initially placing the wire)
    public void UpdateWireEndPoint(Vector2 endPointWorldSpace)
    {
        anchorPoints[anchorPoints.Count - 1] = ProcessPoint(endPointWorldSpace);
        UpdateSmoothedLine();
    }

    // Add anchor point (for when initially placing the wire)
    public void AddAnchorPoint(Vector2 pointWorldSpace)
    {
        anchorPoints[anchorPoints.Count - 1] = ProcessPoint(pointWorldSpace);
        anchorPoints.Add(ProcessPoint(pointWorldSpace));
    }

    private void UpdateCollider()
    {
        wireCollider.points = drawPoints.ToArray();
        wireCollider.edgeRadius = thickness * thicknessMultiplier;
    }

    private void UpdateSmoothedLine()
    {
        length = 0;
        GenerateDrawPoints();

        lineRenderer.positionCount = drawPoints.Count;
        var lastLocalPos = Vector2.zero;
        for (var i = 0; i < lineRenderer.positionCount; i++)
        {
            Vector2 localPos = transform.parent.InverseTransformPoint(drawPoints[i]);
            lineRenderer.SetPosition(i, new Vector3(localPos.x, localPos.y, -0.01f));

            if (i > 0) length += (lastLocalPos - localPos).magnitude;
            lastLocalPos = localPos;
        }
    }

    public void SetSelectionState(bool selected)
    {
        this.selected = selected;
    }

    private Vector2 ProcessPoint(Vector2 endPointWorldSpace)
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            var a = anchorPoints[anchorPoints.Count - 2];
            var b = endPointWorldSpace;
            var mid = (a + b) / 2;

            var xAxisLonger = Mathf.Abs(a.x - b.x) > Mathf.Abs(a.y - b.y);
            if (xAxisLonger)
                return new Vector2(b.x, a.y);
            return new Vector2(a.x, b.y);
        }

        return endPointWorldSpace;
    }

    private void GenerateDrawPoints()
    {
        drawPoints.Clear();
        drawPoints.Add(anchorPoints[0]);

        for (var i = 1; i < anchorPoints.Count - 1; i++)
        {
            var targetPoint = anchorPoints[i];
            var targetDir = (anchorPoints[i] - anchorPoints[i - 1]).normalized;
            var dstToTarget = (anchorPoints[i] - anchorPoints[i - 1]).magnitude;
            var dstToCurveStart = Mathf.Max(dstToTarget - curveSize, dstToTarget / 2);

            var nextTarget = anchorPoints[i + 1];
            var nextTargetDir = (anchorPoints[i + 1] - anchorPoints[i]).normalized;
            var nextLineLength = (anchorPoints[i + 1] - anchorPoints[i]).magnitude;

            var curveStartPoint = anchorPoints[i - 1] + targetDir * dstToCurveStart;
            var curveEndPoint = targetPoint + nextTargetDir * Mathf.Min(curveSize, nextLineLength / 2);

            // Bezier
            for (var j = 0; j < resolution; j++)
            {
                var t = j / (resolution - 1f);
                var a = Vector2.Lerp(curveStartPoint, targetPoint, t);
                var b = Vector2.Lerp(targetPoint, curveEndPoint, t);
                var p = Vector2.Lerp(a, b, t);

                if ((p - drawPoints[drawPoints.Count - 1]).sqrMagnitude > 0.001f) drawPoints.Add(p);
            }
        }

        drawPoints.Add(anchorPoints[anchorPoints.Count - 1]);
    }
}