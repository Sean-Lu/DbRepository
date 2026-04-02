namespace Example.Dapper.Core.Application.Dtos;

public class TestDto
{
    public virtual long UserId { get; set; }
    public virtual string UserName { get; set; }
    public virtual string Email { get; set; }
    public virtual bool IsBlack { get; set; }
    public virtual string Remark { get; set; }
}