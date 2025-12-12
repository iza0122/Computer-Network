using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

// Services/Function2.cs



namespace AgentForMe.Services
{
    /// <summary>
    /// Lớp quản lý các tác vụ/dịch vụ (Windows Services) trên hệ thống.
    /// </summary>
    public class ServiceTaskManager
    {
        // --- 1. DATA TRANSFER OBJECT (DTO) ---

        /// <summary>
        /// Thông tin cơ bản về một Windows Service.
        /// </summary>
        public class ServiceTaskInfo
        {
            public string Name { get; set; }        // Ten Service (vi du: "Dnscache")
            public string DisplayName { get; set; } // Ten hien thi (vi du: "DNS Client")
            public string Status { get; set; }      // Trang thai hien tai (Running, Stopped, v.v.)
        }

        // ----------------------------------------------------------------------------------

        /// <summary>
        /// LIET KE: Lay danh sach cac Windows Services dang chay tren he thong.
        /// </summary>
        /// <returns>Danh sach cac doi tuong ServiceTaskInfo.</returns>
        public List<ServiceTaskInfo> ListServices()
        {
            List<ServiceTaskInfo> serviceList = new List<ServiceTaskInfo>();

            try
            {
                ServiceController[] services = ServiceController.GetServices();

                foreach (ServiceController sc in services.OrderBy(s => s.DisplayName))
                {
                    serviceList.Add(new ServiceTaskInfo
                    {
                        Name = sc.ServiceName,
                        DisplayName = sc.DisplayName,
                        Status = sc.Status.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LIST SERVICE] Loi khi liet ke Services: {ex.Message}");
            }

            return serviceList;
        }

        // ----------------------------------------------------------------------------------

        /// <summary>
        /// CHAY: Khoi dong mot Windows Service.
        /// </summary>
        /// <param name="serviceName">Ten ky thuat cua Service (vi du: 'wuauserv').</param>
        /// <returns>True neu Service da duoc khoi dong thanh cong.</returns>
        public bool StartServiceTask(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName)) return false;

            try
            {
                ServiceController service = new ServiceController(serviceName);
                service.Refresh(); // Cap nhat trang thai moi nhat

                if (service.Status == ServiceControllerStatus.Stopped || service.Status == ServiceControllerStatus.StopPending)
                {
                    // Chay Service va cho toi da 30 giay
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));

                    Console.WriteLine($"[START SERVICE] Service '{serviceName}' da duoc khoi dong thanh cong.");
                    return true;
                }
                else if (service.Status == ServiceControllerStatus.Running)
                {
                    Console.WriteLine($"[START SERVICE] Service '{serviceName}' da chay roi.");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[START SERVICE] Service '{serviceName}' dang o trang thai {service.Status}. Khong the khoi dong.");
                    return false;
                }
            }
            catch (System.InvalidOperationException)
            {
                Console.WriteLine($"[START SERVICE] Loi: Service '{serviceName}' khong ton tai hoac khong co quyen truy cap.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[START SERVICE] Loi khong xac dinh khi khoi dong '{serviceName}': {ex.Message}");
                return false;
            }
        }

        // ----------------------------------------------------------------------------------

        /// <summary>
        /// DUNG: Dung mot Windows Service.
        /// </summary>
        /// <param name="serviceName">Ten ky thuat cua Service.</param>
        /// <returns>True neu Service da duoc dung thanh cong.</returns>
        public bool StopServiceTask(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName)) return false;

            try
            {
                ServiceController service = new ServiceController(serviceName);
                service.Refresh(); // Cap nhat trang thai moi nhat

                if (service.Status == ServiceControllerStatus.Running || service.Status == ServiceControllerStatus.StartPending)
                {
                    if (!service.CanStop)
                    {
                        Console.WriteLine($"[STOP SERVICE] Service '{serviceName}' khong cho phep dung (CanStop = false).");
                        return false;
                    }

                    // Dung Service va cho toi da 30 giay
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));

                    Console.WriteLine($"[STOP SERVICE] Service '{serviceName}' da duoc dung thanh cong.");
                    return true;
                }
                else if (service.Status == ServiceControllerStatus.Stopped)
                {
                    Console.WriteLine($"[STOP SERVICE] Service '{serviceName}' da dung roi.");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[STOP SERVICE] Service '{serviceName}' dang o trang thai {service.Status}. Khong the dung.");
                    return false;
                }
            }
            catch (System.InvalidOperationException)
            {
                Console.WriteLine($"[STOP SERVICE] Loi: Service '{serviceName}' khong ton tai hoac khong co quyen truy cap.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STOP SERVICE] Loi khong xac dinh khi dung '{serviceName}': {ex.Message}");
                return false;
            }
        }
    }
}
