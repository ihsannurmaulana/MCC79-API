﻿using API.Contracts;
using API.Data;
using API.DTOs.Accounts;
using API.Models;
using API.Utilities;
using API.Utilities.Handler;
using System.Security.Claims;

namespace API.Services
{
    public class AccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IUniversityRepository _universityRepository;
        private readonly IEducationRepository _educationRepository;
        private readonly ITokenHandler _tokenHandler;
        private readonly IRoleRepository _roleRepository;
        private readonly IAccountRoleRepository _accountRoleRepository;
        private readonly IEmailHandler _emailHandler;
        private readonly BookingDbContext _bookingDbContext;

        public AccountService(IAccountRepository accountRepository,
            IEmployeeRepository employeeRepository,
            IUniversityRepository universityRepository,
            IEducationRepository educationRepository,
            ITokenHandler tokenHandler,
            IRoleRepository roleRepository,
            IAccountRoleRepository accountRoleRepository,
            IEmailHandler emailHandler,
            BookingDbContext bookingDbContext)
        {
            _accountRepository = accountRepository;
            _employeeRepository = employeeRepository;
            _universityRepository = universityRepository;
            _educationRepository = educationRepository;
            _tokenHandler = tokenHandler;
            _roleRepository = roleRepository;
            _accountRoleRepository = accountRoleRepository;
            _emailHandler = emailHandler;
            _bookingDbContext = bookingDbContext;
        }

        // Register
        public RegisterDto? Register(RegisterDto registerDto)
        {

            if (registerDto.Password != registerDto.ConfirmPassword)
            {
                return null;
            }

            EmployeeService employeeService = new EmployeeService(_employeeRepository, _educationRepository, _universityRepository);

            using var transaction = _bookingDbContext.Database.BeginTransaction();
            try
            {
                Employee employee = new Employee
                {
                    Guid = new Guid(),
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    BirthDate = registerDto.BirthDate,
                    Gender = registerDto.Gender,
                    HiringDate = registerDto.HiringDate,
                    Email = registerDto.Email,
                    PhoneNumber = registerDto.PhoneNumber,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };
                employee.Nik = GenerateNik.Nik(_employeeRepository.GetLastEmployeeNik());

                var createdEmployee = _employeeRepository.Create(employee);
                if (createdEmployee is null)
                {
                    return null;
                }

                var universityEntity = _universityRepository.GetByCodeandName(registerDto.UniversityCode, registerDto.UniversityName);
                if (universityEntity is null)
                {
                    var university = new University
                    {
                        Code = registerDto.UniversityCode,
                        Name = registerDto.UniversityName,
                        Guid = new Guid(),
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now
                    };
                    universityEntity = _universityRepository.Create(university);
                }

                Education education = new Education
                {
                    Guid = employee.Guid,
                    Major = registerDto.Major,
                    Degree = registerDto.Degree,
                    Gpa = registerDto.Gpa,
                    UniversityGuid = universityEntity.Guid
                };

                var createdEducation = _educationRepository.Create(education);
                if (createdEducation is null)
                {
                    return null;
                }


                Account account = new Account
                {
                    Guid = employee.Guid,
                    Password = Hashing.HashPassword(registerDto.Password),
                    //ConfirmPassword = registerDto.ConfirmPassword
                };

                var createdAccount = _accountRepository.Create(account);
                if (createdAccount is null)
                {
                    return null;
                }

                var getRoleUser = _roleRepository.GetByName("User");
                _accountRoleRepository.Create(new AccountRole
                {
                    Guid = new Guid(),
                    AccountGuid = account.Guid,
                    RoleGuid = getRoleUser.Guid
                });


                var toDto = new RegisterDto
                {
                    FirstName = createdEmployee.FirstName,
                    LastName = createdEmployee.LastName,
                    BirthDate = createdEmployee.BirthDate,
                    Gender = createdEmployee.Gender,
                    HiringDate = createdEmployee.HiringDate,
                    Email = createdEmployee.Email,
                    PhoneNumber = createdEmployee.PhoneNumber,
                    Password = createdAccount.Password,
                    Major = createdEducation.Major,
                    Degree = createdEducation.Degree,
                    Gpa = createdEducation.Gpa,
                    UniversityCode = universityEntity.Code,
                    UniversityName = universityEntity.Name,
                };


                transaction.Commit();
                return toDto;
            }
            catch (Exception)
            {
                transaction.Rollback();
                return null;
            }
        }

        // Register Cek
        /*public RegisterDto? Register(RegisterDto registerDto)
        {
            using var transaction = _bookingDbContext.Database.BeginTransaction();
            try
            {
                var employee = new Employee
                {
                    Guid = new Guid(),
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName ?? "",
                    Gender = registerDto.Gender,
                    BirthDate = registerDto.BirthDate,
                    HiringDate = registerDto.HiringDate,
                    Email = registerDto.Email,
                    PhoneNumber = registerDto.PhoneNumber,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                };
                employee.Nik = (_employeeRepository.GetLastEmployeeNik());
                var createdEmployee = _employeeRepository.Create(employee);

                var account = new Account
                {
                    Guid = employee.Guid,
                    Password = Hashing.HashPassword(registerDto.Password),
                    IsDeleted = false,
                    IsUsed = false,
                    Otp = 0,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    ExpiredTime = DateTime.Now.AddYears(5),
                };
                var createdAccount = _accountRepository.Create(account);

                var roleUser = _roleRepository.GetByName("User");
                _accountRoleRepository.Create(new AccountRole
                {
                    AccountGuid = account.Guid,
                    RoleGuid = roleUser.Guid
                });

                var universityEntity = _universityRepository.GetByCodeandName(registerDto.UniversityCode, registerDto.UniversityName);
                if (universityEntity == null)
                {
                    var university = new University
                    {
                        Code = registerDto.UniversityCode,
                        Name = registerDto.UniversityName,
                        Guid = new Guid(),
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now,
                    };
                    universityEntity = _universityRepository.Create(university);
                }

                var education = new Education
                {
                    Guid = employee.Guid,
                    Degree = registerDto.Degree,
                    Major = registerDto.Major,
                    Gpa = registerDto.Gpa,
                    UniversityGuid = universityEntity.Guid,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                };
                var createdEducation = _educationRepository.Create(education);

                var toDto = new RegisterDto
                {
                    FirstName = createdEmployee.FirstName,
                    LastName = createdEmployee.LastName,
                    BirthDate = createdEmployee.BirthDate,
                    Gender = createdEmployee.Gender,
                    HiringDate = createdEmployee.HiringDate,
                    Email = createdEmployee.Email,
                    PhoneNumber = createdEmployee.PhoneNumber,
                    Password = createdAccount.Password,
                    Major = createdEducation.Major,
                    Degree = createdEducation.Degree,
                    Gpa = createdEducation.Gpa,
                    UniversityCode = universityEntity.Code,
                    UniversityName = universityEntity.Name,
                };

                transaction.Commit();
                return toDto;
            }
            catch
            {
                transaction.Rollback();
                return null;
            }
        }*/
        // Login Service
        public string Login(LoginDto loginDto)
        {
            var employee = _employeeRepository.GetEmployeeByEmail(loginDto.Email);
            if (employee == null)
            {
                return "0";
            }

            var password = _accountRepository.GetByGuid(employee.Guid);
            var isPasswordValid = Hashing.ValidatePassword(loginDto.Password, password!.Password);
            if (!isPasswordValid)
            {
                return "-1";
            }


            try
            {
                var claims = new List<Claim>()
                {
                new Claim("NIK", employee.Nik),
                new Claim("FullName", $"{employee.FirstName} {employee.LastName}"),
                new Claim("Email", loginDto.Email)
                };

                var getAccountRole = _accountRoleRepository.GetAccountRolesByAccountGuid(employee.Guid);
                var getRoleNameByAccountRole = from ar in getAccountRole
                                               join r in _roleRepository.GetAll() on ar.RoleGuid equals r.Guid
                                               select r.Name;

                claims.AddRange(getRoleNameByAccountRole.Select(role => new Claim(ClaimTypes.Role, role)));

                var getToken = _tokenHandler.GenerateToken(claims);
                return getToken;
            }
            catch (Exception)
            {

                return "-2";
            }

            /*var toDto = new LoginDto
            {
                Email = loginDto.Email,
                Password = loginDto.Password,
            };

            return toDto;*/
        }

        // Forget Password Service
        public int ForgetPassword(ForgetPasswordDto forgetPassword)
        {
            var employee = _employeeRepository.GetEmployeeByEmail(forgetPassword.Email);
            if (employee == null)
            {
                return -1;
            }

            Random rand = new Random();
            HashSet<int> uniqueDigits = new HashSet<int>();

            while (uniqueDigits.Count < 6)
            {
                int digit = rand.Next(0, 9);
                uniqueDigits.Add(digit);
            }

            int generateOtp = uniqueDigits.Aggregate(0, (acc, digit) => acc * 10 + digit);

            var relatedAccount = GetAccount(employee.Guid);

            var updateAccountDto = new AccountDto
            {
                Guid = relatedAccount.Guid,
                Password = relatedAccount.Password,
                IsDeleted = relatedAccount.IsDeleted,
                Otp = generateOtp,
                IsUsed = false,
                ExpiredTime = DateTime.Now.AddMinutes(5)
            };
            var updateResult = UpdateAccount(updateAccountDto);
            if (updateResult == 0)
            {
                return 0;
            }

            _emailHandler.SendEmail(forgetPassword.Email,
                "Forgot Password",
                $"Your OTP is {updateAccountDto.Otp}");

            return 1;
        }

        // Change Password
        public int ChangePassword(ChangePasswordDto changePasswordDto)
        {
            var isExist = _employeeRepository.GetEmployeeByEmail(changePasswordDto.Email);
            if (isExist is null)
                return -1;

            var getAccount = _accountRepository.GetByGuid(isExist.Guid);
            if (getAccount.Otp != changePasswordDto.Otp)
                return 0;

            if (getAccount.IsUsed == true)
                return 1;

            if (getAccount.ExpiredTime < DateTime.Now)
                return 2;

            var account = new Account
            {
                Guid = getAccount.Guid,
                IsUsed = getAccount.IsUsed,
                IsDeleted = getAccount.IsDeleted,
                ModifiedDate = DateTime.Now,
                CreatedDate = getAccount!.CreatedDate,
                Otp = changePasswordDto.Otp,
                ExpiredTime = getAccount.ExpiredTime,
                Password = Hashing.HashPassword(changePasswordDto.NewPassword)
            };
            var isUpdated = _accountRepository.Update(account);
            if (!isUpdated)
            {
                return 0;
            }
            return 3;
        }

        public IEnumerable<AccountDto>? GetAccount()
        {
            var accounts = _accountRepository.GetAll();
            if (!accounts.Any())
            {
                return null; // No Account  found
            }
            /*foreach (var account in accounts)
            {
                Console.WriteLine(account.ExpiredTime);
            }*/

            var toDto = accounts.Select(account => new AccountDto

            {
                Guid = account.Guid,
                Password = account.Password,
                Otp = account.Otp,
                IsDeleted = account.IsDeleted,
                IsUsed = account.IsUsed,
                ExpiredTime = account.ExpiredTime,
            }).ToList();

            return toDto; // Account found
        }

        public AccountDto? GetAccount(Guid guid)
        {
            var account = _accountRepository.GetByGuid(guid);
            if (account is null)
            {
                return null; // account not found
            }

            var toDto = new AccountDto
            {
                Guid = account.Guid,
                Password = account.Password,
                Otp = account.Otp,
                IsDeleted = account.IsDeleted,
                IsUsed = account.IsUsed,
                ExpiredTime = account.ExpiredTime,
            };

            return toDto; // accounts found
        }

        public AccountDto? CreateAccount(NewAccountDto newAccountDto)
        {
            var account = new Account
            {
                Guid = newAccountDto.Guid,
                Password = Hashing.HashPassword(newAccountDto.Password),
                Otp = newAccountDto.Otp,
                IsDeleted = newAccountDto.IsDeleted,
                IsUsed = newAccountDto.IsUsed,
                ExpiredTime = DateTime.Now,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            var createdAccount = _accountRepository.Create(account);
            if (createdAccount is null)
            {
                return null; // Account not created
            }

            var toDto = new AccountDto
            {
                Guid = createdAccount.Guid,
                Password = createdAccount.Password,
                Otp = createdAccount.Otp,
                IsDeleted = createdAccount.IsDeleted,
                IsUsed = createdAccount.IsUsed,
                ExpiredTime = createdAccount.ExpiredTime,
            };

            return toDto; // Account created
        }

        public int UpdateAccount(AccountDto updateAccountDto)
        {
            var isExist = _accountRepository.IsExist(updateAccountDto.Guid);
            if (!isExist)
            {
                return -1; // Account not found
            }

            var getAccount = _accountRepository.GetByGuid(updateAccountDto.Guid);

            var account = new Account
            {
                Guid = updateAccountDto.Guid,
                Password = Hashing.HashPassword(updateAccountDto.Password),
                Otp = updateAccountDto.Otp,
                IsUsed = updateAccountDto.IsUsed,
                IsDeleted = updateAccountDto.IsDeleted,
                ExpiredTime = updateAccountDto.ExpiredTime,
                ModifiedDate = DateTime.Now,
                CreatedDate = getAccount!.CreatedDate
            };

            var isUpdate = _accountRepository.Update(account);
            if (!isUpdate)
            {
                return 0; // Account not updated
            }

            return 1;
        }

        public int DeleteAccount(Guid guid)
        {
            var isExist = _accountRepository.IsExist(guid);
            if (!isExist)
            {
                return -1; // Account not found
            }

            var account = _accountRepository.GetByGuid(guid);
            var isDelete = _accountRepository.Delete(account!);
            if (!isDelete)
            {
                return 0; // Account not deleted
            }

            return 1;
        }
    }


}
