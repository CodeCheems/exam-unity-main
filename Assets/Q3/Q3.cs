using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

/**
**该题目校招岗位可以不作答，社招需要作答。**

按照要求在 {@link Q3.onStartBtnClick} 中编写一段异步任务处理逻辑，具体执行步骤如下：
1. 调用 {@link Q3.loadConfig} 加载配置文件，获取资源列表
2. 根据资源列表调用 {@link Q3.loadFile} 加载资源文件
3. 资源列表中的所有文件加载完毕后，调用 {@link Q3.initSystem} 进行系统初始化
4. 系统初始化完成后，打印日志

附加要求
1. 加载文件时，需要做并发控制，最多并发 3 个文件
2. 加载文件时，需要添加超时控制，超时时间为 3 秒
3. 加载文件失败时，需要对单文件做 backoff retry 处理，重试次数为 3 次
4. 对错误进行捕获并打印输出
*/

public class Q3 : MonoBehaviour
{
    public async void OnStartBtnClick()
    {
        const int maxConcurrency = 3;
        const int timeoutMs = 3000;
        const int maxRetries = 3;
        const int initialBackoffMs = 500;

        SemaphoreSlim semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        string[] files;
        try
        {
            files = await LoadConfig();
        }
        catch (Exception ex)
        {
            Debug.LogError($"LoadConfig failed: {ex}");
            return;
        }

        List<Task> tasks = new List<Task>();

        foreach (var file in files)
        {
            tasks.Add(ProcessFileAsync(file));
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            // ProcessFileAsync 已捕获并记录单个文件的异常，这里做兜底日志
            Debug.LogError($"Unexpected error when loading files: {ex}");
        }

        try
        {
            await InitSystem();
            Debug.Log("All done: system initialized.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"InitSystem failed: {ex}");
        }

        // 本地函数：处理单文件的并发、超时与重试逻辑
        async Task ProcessFileAsync(string file)
        {
            bool success = false;
            int attempt = 0;
            int backoff = initialBackoffMs;

            while (attempt < maxRetries && !success)
            {
                attempt++;

                await semaphore.WaitAsync();
                try
                {
                    Debug.Log($"Start loading file '{file}', attempt {attempt}.");
                    var loadTask = LoadFile(file);
                    var completed = await Task.WhenAny(loadTask, Task.Delay(timeoutMs));
                    if (completed == loadTask)
                    {
                        // 确保如果 LoadFile 抛异常能被捕获
                        try
                        {
                            await loadTask; // propagate exceptions if any
                            success = true;
                            Debug.Log($"File '{file}' loaded successfully on attempt {attempt}.");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"LoadFile exception for '{file}' on attempt {attempt}: {ex}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"LoadFile timeout for '{file}' on attempt {attempt} (>{timeoutMs}ms).");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Unexpected error while loading '{file}' on attempt {attempt}: {ex}");
                }
                finally
                {
                    semaphore.Release();
                }

                if (!success)
                {
                    if (attempt < maxRetries)
                    {
                        Debug.Log($"Will retry '{file}' after {backoff}ms backoff (attempt {attempt + 1}).");
                        await Task.Delay(backoff);
                        backoff *= 2; // 指数回退
                    }
                    else
                    {
                        Debug.LogError($"Failed to load '{file}' after {maxRetries} attempts.");
                    }
                }
            }
        }
    }

    // #region 以下是辅助测试题而写的一些 mock 函数，请勿修改

    /// <summary>
    /// 加载配置文件
    /// </summary>
    /// <returns>文件列表</returns>
    public async Task<string[]> LoadConfig()
    {
        Debug.Log("load config start");
        await Task.Delay(1000);
        if (Random.value > 0.01f)
        {
            Debug.Log("load config success");
            string[] files = new string[100];
            for (int i = 0; i < 100; i++)
            {
                files[i] = $"file-{i}";
            }
            return files;
        }
        else
        {
            Debug.Log("load config failed");
            throw new System.Exception("Load config failed");
        }
    }

    /// <summary>
    /// 加载文件
    /// </summary>
    /// <param name="file">文件名</param>
    /// <returns></returns>
    public async Task LoadFile(string file)
    {
        Debug.Log($"load file start: {file}");
        await Task.Delay(Random.Range(1000, 5000));
        if (Random.value > 0.01f)
        {
            Debug.Log($"load file success: {file}");
        }
        else
        {
            Debug.Log($"load file failed: {file}");
            throw new System.Exception($"Load file failed: {file}");
        }
    }

    /// <summary>
    /// 初始化系统
    /// </summary>
    /// <returns></returns>
    public async Task InitSystem()
    {
        Debug.Log("init system start");
        await Task.Delay(1000);
        Debug.Log("init system success");
    }

    // #endregion
}
