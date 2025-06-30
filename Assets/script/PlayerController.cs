using UnityEngine;

public class PatrolController : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public float moveSpeed = 2f;
    public float rotateSpeed = 180f;

    private enum Mode { Manual, Auto }
    private Mode currentMode = Mode.Manual;

    private enum AutoState { Idle, Rotating, Moving }
    private AutoState autoState = AutoState.Idle;

    private Vector3 targetPos;
    private Quaternion targetRot;
    private bool goingToEnd = true;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (currentMode == Mode.Manual)
            {
                currentMode = Mode.Auto;
                EnterAutoMode();
            }
            else
            {
                currentMode = Mode.Manual;
            }
        }

        if (currentMode == Mode.Manual)
        {
            HandleManual();
        }
        else
        {
            HandleAuto();
        }
    }

    void EnterAutoMode()
    {
        transform.position = startPoint.position;

        // 初始朝向 = 看向终点
        Vector3 toEnd = endPoint.position - startPoint.position;
        if (toEnd != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(toEnd.normalized);

        // 再旋转 +90 度（右转）
        transform.Rotate(0, 90f, 0);

        // 设置自动巡逻状态
        goingToEnd = true;
        autoState = AutoState.Moving;
        targetPos = endPoint.position;
    }

    void HandleManual()
    {
        float h = Input.GetAxis("Horizontal"); // A/D
        float v = Input.GetAxis("Vertical");   // W/S
        Vector3 move = transform.forward * v + transform.right * h;
        transform.position += move * moveSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.Q))
            transform.Rotate(Vector3.up, -rotateSpeed * Time.deltaTime);
        if (Input.GetKey(KeyCode.E))
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    void HandleAuto()
    {
        switch (autoState)
        {
            case AutoState.Moving:
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, targetPos) < 0.01f)
                {
                    // 到达目标，准备旋转 180°
                    targetRot = Quaternion.Euler(0, transform.eulerAngles.y + 180f, 0);
                    autoState = AutoState.Rotating;
                }
                break;

            case AutoState.Rotating:
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
                if (Quaternion.Angle(transform.rotation, targetRot) < 0.5f)
                {
                    autoState = AutoState.Moving;

                    // 设置下一个目标点
                    goingToEnd = !goingToEnd;
                    targetPos = goingToEnd ? endPoint.position : startPoint.position;
                }
                break;
        }
    }
}
