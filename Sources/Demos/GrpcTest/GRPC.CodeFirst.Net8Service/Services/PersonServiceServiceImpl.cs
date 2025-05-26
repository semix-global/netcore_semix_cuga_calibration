using GRPC.CodeFirst.Shared;
using OpenCvSharp;
using ProtoBuf.Grpc;

namespace GRPC.CodeFirst.Net8Service.Services;

public sealed class PersonServiceServiceImpl : IPersonService
{
    public Person Get(ParamWrapper paramWrapper)
    {
        return new Person
        {
            Id = paramWrapper.Id ?? 1,
            Name = paramWrapper.Name ?? "TestObj",
            TagEnum = paramWrapper.TagEnum ?? TagEnum.Tag1 | TagEnum.Tag3 | TagEnum.Tag2,
            DateTime = paramWrapper.DateTime ?? DateTime.Now,
            Guid = paramWrapper.Guid ?? Guid.NewGuid(),
            Address = new Address
            {
                Line1 = "Flat 1",
                Line2 = "The Meadows",
                Image = paramWrapper.Image ??
                        (paramWrapper.IsRandom
                            ? GetRandomImage()
                            : paramWrapper.IsLarge
                                ? Convert.FromBase64String(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\TestLarge.txt")))
                                : Convert.FromBase64String(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\Test.txt"))))
            }
        };
    }

    public async Task<Person> GetCallContextAsync(IntWrapper intWrapper, CallContext context)
    {
        try
        {
            await Task.Delay(5000, context.CancellationToken).ConfigureAwait(false);

            return new Person
            {
                Id = intWrapper.Value,
                Name = "TestObj",
                TagEnum = TagEnum.Tag1 | TagEnum.Tag3 | TagEnum.Tag2,
                DateTime = DateTime.Now,
                Guid = Ulid.NewUlid().ToGuid(),
                Address = new Address
                {
                    Line1 = "Flat 1",
                    Line2 = "The Meadows",
#if NET
                    Image = Convert.FromBase64String(await File.ReadAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\Test.txt"), context.CancellationToken).ConfigureAwait(false))
#else
                    Image = Convert.FromBase64String(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\Test.txt")))
#endif
                }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    public async Task<Person> GetCancellationTokenAsync(IntWrapper intWrapper, CancellationToken context = default)
    {
        try
        {
            await Task.Delay(5000, context).ConfigureAwait(false);

            return new Person
            {
                Id = intWrapper.Value,
                Name = "TestObj",
                TagEnum = TagEnum.Tag1 | TagEnum.Tag3 | TagEnum.Tag2,
                DateTime = DateTime.Now,
                Guid = Ulid.NewUlid().ToGuid(),
                Address = new Address
                {
                    Line1 = "Flat 1",
                    Line2 = "The Meadows",
#if NET
                    Image = Convert.FromBase64String(await File.ReadAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\Test.txt"), context).ConfigureAwait(false))
#else
                    Image = Convert.FromBase64String(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\Test.txt")))
#endif
                }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    public SxExecuteRet<List<int>> GetList()
    {
        return new SxExecuteRet<List<int>>
        {
            Success = true,
            Anything = [1, 2, 3, 4, 5]
        };
    }

    public SxExecuteRet<List<Person>> GetListByParam(TestObj testObj)
    {
        return new SxExecuteRet<List<Person>>
        {
            Success = testObj.Success,
            Msg = testObj.Msg,
            Anything =
            [
                new Person
                {
                    Id = 1,
                    Name = "TestObj",
                    TagEnum = TagEnum.Tag1 | TagEnum.Tag3 | TagEnum.Tag2,
                    DateTime = DateTime.Now,
                    Guid = Ulid.NewUlid().ToGuid(),
                    Address = new Address
                    {
                        Line1 = "Flat 1",
                        Line2 = "The Meadows",
                        Image = [1, 2]
                    }
                },
                new Person
                {
                    Id = 2,
                    Name = "Test2",
                    TagEnum = TagEnum.Tag1 | TagEnum.Tag3 | TagEnum.Tag2,
                    DateTime = DateTime.Now,
                    Guid = Ulid.NewUlid().ToGuid(),
                    Address = new Address
                    {
                        Line1 = "Flat 2",
                        Line2 = "The Meadows 2",
                        Image = [3, 4]
                    }
                },
                new Person
                {
                    Id = 3,
                    Name = "TestObj 3",
                    TagEnum = TagEnum.Tag1 | TagEnum.Tag3 | TagEnum.Tag2,
                    DateTime = DateTime.Now,
                    Guid = Ulid.NewUlid().ToGuid(),
                    Address = new Address
                    {
                        Line1 = "Flat 3",
                        Line2 = "The Meadows 3",
                        Image = [5, 6]
                    }
                },
                new Person
                {
                    Id = 4,
                    Name = "TestObj 4",
                    TagEnum = TagEnum.Tag1 | TagEnum.Tag2,
                    DateTime = DateTime.Now,
                    Guid = Ulid.NewUlid().ToGuid(),
                    Address = new Address
                    {
                        Line1 = "Flat 4",
                        Line2 = "The Meadows 4",
                        Image = []
                    }
                }
            ]
        };
    }

    public Task<SxExecuteRet<TestObj>> GetListByParam1Async(CancellationToken context = default)
    {
        return Task.FromResult(new SxExecuteRet<TestObj> { Success = true, Msg = "Ok", Anything = new TestObj { Success = true, Msg = "Ok1", Tes = ["OK2", "OK3"] } });
    }

    public Task<SxExecuteRet<SxExecuteRet<TestObj>>> GetListByParam2Async(CancellationToken context = default)
    {
        return Task.FromResult(new SxExecuteRet<SxExecuteRet<TestObj>> { Success = true, Msg = "Ok1", Anything = new SxExecuteRet<TestObj> { Success = true, Msg = "Ok2", Anything = new TestObj { Success = true, Msg = "Ok3", Tes = ["OK4", "OK5"] } } });
    }

    public SxExecuteRet<List<TestObj1>> GetList1()
    {
        return new SxExecuteRet<List<TestObj1>>
        {
            Success = true,
            Anything =
            [
                new TestObj1
                {
                    Id = 1,
                    Name = "1"
                }
            ]
        };
    }

    private static byte[] GetRandomImage()
    {
        Thread.Sleep(30);
        /*const int width = 8500;
        const int height = 8500;*/
        const int width = 2448;
        const int height = 2048;

        // 创建黑色背景的图像
        using var mat = new Mat(height, width, MatType.CV_8UC3, Scalar.Black);
        var random = new Random();

        for (var i = 0; i < 10; i++)
        {
            var shapeType = random.Next(3); // 0: 矩形, 1: 圆形, 2: 椭圆
            var x = random.Next(width);
            var y = random.Next(height);
            var size = random.Next(50, 200); // 形状大小
            var color = Scalar.White;

            switch (shapeType)
            {
                case 0: // 矩形
                    Cv2.Rectangle(mat, new Rect(x, y, size, size), color, -1);
                    break;

                case 1: // 圆形
                    Cv2.Circle(mat, new Point(x + size / 2, y + size / 2), size / 2, color, -1);
                    break;

                case 2: // 椭圆
                    Cv2.Ellipse(mat, new Point(x + size / 2, y + size / 4), new Size(size / 2, size / 4), 0, 0, 360, color, -1);
                    break;
            }
        }

        // 将图像保存到内存流中
        using var memoryStream = new MemoryStream();
        Cv2.ImEncode(".bmp", mat, out var imageBytes);
        memoryStream.Write(imageBytes, 0, imageBytes.Length);
        return memoryStream.ToArray();
    }
}