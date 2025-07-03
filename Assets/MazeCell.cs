using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeCell : MonoBehaviour
{
    [SerializeField]
    private GameObject _leftWall;

    [SerializeField]
    private GameObject _rightWall;

    [SerializeField]
    private GameObject _frontWall;

    [SerializeField]
    private GameObject _backWall;

    [SerializeField]
    private GameObject _unvisitedBlock;

    public bool IsVisited { get; private set; }

    public bool HasLeftWall { get; private set; } = true;
    public bool HasRightWall { get; private set; } = true;
    public bool HasFrontWall { get; private set; } = true;
    public bool HasBackWall { get; private set; } = true;

    public GameObject leftWall;
    public GameObject rightWall;
    public GameObject frontWall;
    public GameObject backWall;

    

    public void Visit()
    {
        IsVisited = true;
        if (_unvisitedBlock != null)
            _unvisitedBlock.SetActive(false);
    }

    public void ClearLeftWall()
    {
        if (_leftWall != null) _leftWall.SetActive(false);
        HasLeftWall = false;
    }

    public void ClearRightWall()
    {
        if (_rightWall != null) _rightWall.SetActive(false);
        HasRightWall = false;
    }

    public void ClearFrontWall()
    {
        if (_frontWall != null) _frontWall.SetActive(false);
        HasFrontWall = false;
    }

    public void ClearBackWall()
    {
        if (_backWall != null) _backWall.SetActive(false);
        HasBackWall = false;
    }

    public void SetLeftWall(bool active)
{
    if (_leftWall != null) _leftWall.SetActive(active);
    HasLeftWall = active;
}

public void SetRightWall(bool active)
{
    if (_rightWall != null) _rightWall.SetActive(active);
    HasRightWall = active;
}

public void SetFrontWall(bool active)
{
    if (_frontWall != null) _frontWall.SetActive(active);
    HasFrontWall = active;
}

public void SetBackWall(bool active)
{
    if (_backWall != null) _backWall.SetActive(active);
    HasBackWall = active;
}
}
