using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace QueDuler.Core.Test;

public class SerializationTests
{
    [Fact]
    public void basicSerilize()
    {
        var sea = "{\"IsRetry\":false,\"JobId\":\"JobId\",\"RetryState\":null,\"IsBroadCast\":false,\"ArgumentObject\":{\"Id\":1,\"basketId\":\"basket1\"}}";

        var t = new DispatchableJobArgument("JobId", new { Id = 1, basketId = "basket1" }, false);

        var j = t.ToJson();
        j.Should().Be(sea);
        var arg = JsonConvert.DeserializeObject<DispatchableJobArgument>(j);
        var obj = JsonConvert.DeserializeObject<TestObj>(JsonConvert.SerializeObject(arg.ArgumentObject));

        obj.BasketId.Should().Be("basket1");
        obj.Id.Should().Be(1);


    }
    [Fact]
    public void GlobalSerializerSerilize()
    {
        var sea = "{\"is_retry\":false,\"job_id\":\"JobId\",\"retry_state\":null,\"is_broad_cast\":false,\"argument_object\":{\"id\":1,\"basket_id\":\"basket1\"}}";

        var t = new DispatchableJobArgument("JobId", new { Id = 1, basketId = "basket1" }, false);
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };

        var j = t.ToJson();
        j.Should().Be(sea);
        var arg = JsonConvert.DeserializeObject<DispatchableJobArgument>(j);
        var obj = JsonConvert.DeserializeObject<TestObj>(JsonConvert.SerializeObject(arg.ArgumentObject));

        obj.BasketId.Should().Be("basket1");
        obj.Id.Should().Be(1);


    }
    [Fact]
    public void RetrySerilize()
    {
        var sea = "{\"IsRetry\":false,\"JobId\":\"JobId\",\"RetryState\":null,\"IsBroadCast\":false,\"ArgumentObject\":{\"Id\":1,\"basketId\":\"basket1\"}}";

        var t = new DispatchableJobArgument("JobId", new { Id = 1, basketId = "basket1" }, false);
        var retry = t.GetRetryObjectForJob("another","r");
        var j = retry.ToJson();
        //j.Should().Be(sea);
        var arg = JsonConvert.DeserializeObject<DispatchableJobArgument>(j);
        var obj = JsonConvert.DeserializeObject<TestObj>(JsonConvert.SerializeObject(arg.ArgumentObject));

        obj.BasketId.Should().Be("basket1");
        obj.Id.Should().Be(1);
        arg.IsRetry.Should().BeTrue();
        arg.RetryState.Should().Be("r");
        arg.JobId.Should().Be("another");

    }

    class TestObj
    {
        public int Id { get; set; }
        public string BasketId { get; set; }
    }
}