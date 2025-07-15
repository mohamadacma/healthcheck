using Microsoft.EntityFrameworkCore;
using HealthCheckApi.Models;
using Xunit;


namespace HealthCheckApi.Tests.Models
{

public class ItemTests 
{
    [Fact]
    public void Item_ParamConstructor_InitPropertiesCorrectly()
    {
        //Arrange
        var name = "Test Item";
        var quantity = 8;
        var beforeCreation = DateTime.UtcNow;

        //Act
        var item = new Item(name,quantity);

        //Assert
        Assert.Equal(name,item.Name);
        Assert.Equal(quantity, item.Quantity);
        Assert.True(item.LastUpdated >= beforeCreation);
        Assert.True(item.LastUpdated <= DateTime.UtcNow);
        Assert.Equal(0, item.Id);
    }

    [Fact]
    public void Item_DefaultConstructor_InitializeCorrectly()
    {
        //Arrange and Act
        var item = new Item();

        //Assert
        Assert.Equal(0,item.Id);
        Assert.Equal(string.Empty, item.Name);
        Assert.Equal(0,item.Quantity);
        Assert.Equal(default(DateTime), item.LastUpdated);
    }
    [Fact]
    public void Item_SetProperties_ValuesUpdateCorrectly()
    {
        //Arrange
        var item = new Item();
        String newName = "NewItem";
        int newQuantity = 7;
        var newDate = DateTime.UtcNow.AddDays(-1);

        //Act
        item.Id = 1;
        item.Name= newName;
        item.Quantity = newQuantity;
        item.LastUpdated = newDate;

        //Assert
        Assert.Equal(1,item.Id);
        Assert.Equal(newName, item.Name);
        Assert.Equal(newQuantity, item.Quantity);
        Assert.Equal(newDate, item.LastUpdated);

    }

}
}