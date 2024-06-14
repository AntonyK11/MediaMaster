namespace MediaMaster.DataBase.Models;

public class TagTag
{
    public virtual int ChildrenTagId { get; set; }
    public virtual int ParentsTagId { get; set; }
}