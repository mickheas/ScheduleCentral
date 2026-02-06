// import React, { Fragment, useState } from 'react';
// import { NavLink } from 'react-router-dom';
// import { HomeIcon, CalendarIcon, BuildingOffice2Icon, BookOpenIcon, UserIcon, CogIcon, Bars3Icon, XMarkIcon } from '@heroicons/react/24/outline';
// import { Transition, Dialog } from '@headlessui/react';

// // Define the session prop type
// interface AppSession {
//     isAuthenticated: boolean;
//     email: string;
//     firstName: string;
//     roles: string[];
// }

// interface DashboardLayoutProps {
//     session: AppSession;
//     children: React.ReactNode;
// }

// // Helper to check roles
// const hasRole = (session: AppSession, roles: string[]) => {
//     return session.roles.some(role => roles.includes(role));
// }

// const DashboardLayout: React.FC<DashboardLayoutProps> = ({ session, children }) => {
//     const [sidebarOpen, setSidebarOpen] = useState(false);

//     // Define navigation based on roles
//     const navigation = [
//         { name: 'Dashboard', href: '/', icon: HomeIcon, show: true },
//         { name: 'My Schedule', href: '/MySchedule', icon: CalendarIcon, show: hasRole(session, ['Instructor']) },
//         { name: 'View Schedule', href: '/ViewSchedule', icon: CalendarIcon, show: hasRole(session, ['Student']) },
//         { name: 'Manage Schedules', href: '/ManageSchedules', icon: CalendarIcon, show: hasRole(session, ['ProgramOfficer', 'Admin']) },
//         { name: 'Manage Rooms', href: '/ManageRooms', icon: BuildingOffice2Icon, show: hasRole(session, ['ProgramOfficer', 'Admin']) },
//         { name: 'Manage Courses', href: '/ManageCourses', icon: BookOpenIcon, show: hasRole(session, ['ProgramOfficer', 'Admin']) },
//         // .NET Admin link. 'href' points to the .NET controller
//         { name: 'Admin Panel', href: '/Admin', icon: CogIcon, show: hasRole(session, ['Admin']), isExternal: true },
//     ];

//     const classNames = (...classes: string[]) => classes.filter(Boolean).join(' ');

//     const renderNavLinks = () => (
//         <nav className="flex-1 space-y-1 px-2 py-4">
//             {navigation.filter(item => item.show).map((item) => (
//                 <a
//                     key={item.name}
//                     href={item.href} // Use <a> for external, NavLink for internal
//                     className={classNames(
//                         'text-gray-600 hover:bg-gray-50 hover:text-gray-900',
//                         'group flex items-center px-2 py-2 text-sm font-medium rounded-md'
//                     )}
//                 >
//                     <item.icon
//                         className={classNames('text-gray-400 group-hover:text-gray-500', 'mr-3 flex-shrink-0 h-6 w-6')}
//                         aria-hidden="true"
//                     />
//                     {item.name}
//                 </a>
//             ))}
//         </nav>
//     );

//     return (
//         <div className="min-h-[calc(100vh-65px)]"> {/* Full height minus navbar */}
//             {/* Mobile sidebar */}
//             <Transition.Root show={sidebarOpen} as={Fragment}>
//                 <Dialog as="div" className="relative z-40 md:hidden" onClose={setSidebarOpen}>
//                     {/* ... (Mobile dialog overlay) ... */}
//                     <div className="fixed inset-0 z-40 flex">
//                         <Transition.Child as={Fragment} /* ... (enter/leave transitions) ... */>
//                             <Dialog.Panel className="relative flex w-full max-w-xs flex-1 flex-col bg-white">
//                                 <div className="absolute top-0 right-0 -mr-12 pt-2">
//                                     <button type="button" onClick={() => setSidebarOpen(false)} /* ... */>
//                                         <XMarkIcon className="h-6 w-6 text-white" />
//                                     </button>
//                                 </div>
//                                 <div className="h-0 flex-1 overflow-y-auto pt-5 pb-4">
//                                     {renderNavLinks()}
//                                 </div>
//                             </Dialog.Panel>
//                         </Transition.Child>
//                     </div>
//                 </Dialog>
//             </Transition.Root>

//             {/* Static sidebar for desktop */}
//             <div className="hidden md:fixed md:inset-y-[65px] md:flex md:w-64 md:flex-col"> {/* 65px = navbar height */}
//                 <div className="flex min-h-0 flex-1 flex-col border-r border-gray-200 bg-white">
//                     <div className="flex flex-1 flex-col overflow-y-auto pt-5 pb-4">
//                         {renderNavLinks()}
//                     </div>
//                 </div>
//             </div>

//             {/* Main content area */}
//             <div className="md:pl-64 flex flex-col h-full">
//                 <div className="sticky top-0 z-10 bg-white pl-1 pt-1 sm:pl-3 sm:pt-3 md:hidden">
//                     <button type="button" onClick={() => setSidebarOpen(true)} className="-ml-0.5 -mt-0.5 inline-flex h-12 w-12 items-center justify-center rounded-md text-gray-500 ...">
//                         <Bars3Icon className="h-6 w-6" />
//                     </button>
//                 </div>
//                 <main className="flex-1 p-4 md:p-8">
//                     {/* This is where the <Routes> from App.tsx will render their content */}
//                     {children}
//                 </main>
//             </div>
//         </div>
//     );
// };

// export default DashboardLayout;
import { Dialog, Transition } from '@headlessui/react';
import {
    AcademicCapIcon,
    ArrowLeftOnRectangleIcon,
    Bars3Icon,
    BuildingOfficeIcon,
    CalendarIcon,
    ChartPieIcon,
    ClipboardDocumentListIcon,
    Cog6ToothIcon,
    HomeIcon,
    UserGroupIcon,
    XMarkIcon
} from '@heroicons/react/24/outline';
import * as React from 'react';
import { Fragment, useState } from 'react';
import { NavLink } from 'react-router-dom';

    interface AppSession {
    isAuthenticated: boolean;
    userName: string;
    email: string;
    roles: string[];
    }

    interface LayoutProps {
    session: AppSession;
    children: React.ReactNode;
    }

    const DashboardLayout: React.FC<LayoutProps> = ({ session, children }) => {
    const [sidebarOpen, setSidebarOpen] = useState(false);

    const hasRole = (requiredRoles: string[]) => {
        if (requiredRoles.includes('All')) return true; 
        return session.roles.some(role => requiredRoles.includes(role));
    };

    const navigation = [
        { name: 'Dashboard', href: '/', icon: HomeIcon, roles: ['All'] },
        
        // Top Management
        { name: 'Analytics & Reports', href: '/tm/reports', icon: ChartPieIcon, roles: ['TopManagement', 'Admin'] },
        
        // Department / PO
        { name: 'Departments', href: '/dept/management', icon: BuildingOfficeIcon, roles: ['Department', 'ProgramOfficer', 'Admin'] },
        { name: 'Courses', href: '/po/courses', icon: AcademicCapIcon, roles: ['ProgramOfficer', 'Admin', 'Department'] },
        { name: 'Rooms', href: '/po/rooms', icon: BuildingOfficeIcon, roles: ['ProgramOfficer', 'Admin', 'Department'] },
        { name: 'Generate Schedule', href: '/po/generate', icon: CalendarIcon, roles: ['ProgramOfficer', 'Admin', 'Department'] },

        // Instructor
        { name: 'My Schedule', href: '/instructor/schedule', icon: CalendarIcon, roles: ['Instructor', 'Admin'] },
        { name: 'Swap Requests', href: '/instructor/requests', icon: ClipboardDocumentListIcon, roles: ['Instructor', 'Admin'] },

        // Admin
        { name: 'Users', href: '/admin/users', icon: UserGroupIcon, roles: ['Admin'] },
        { name: 'Settings', href: '/admin/settings', icon: Cog6ToothIcon, roles: ['Admin'] },
    ];

    const filteredNav = navigation.filter(item => hasRole(item.roles));
    
    // Helper for active link classes
    const getNavLinkClass = (isActive: boolean) => {
        return isActive
        ? 'bg-gray-800 text-white group flex gap-x-3 rounded-md p-2 text-sm leading-6 font-semibold'
        : 'text-gray-400 hover:text-white hover:bg-gray-800 group flex gap-x-3 rounded-md p-2 text-sm leading-6 font-semibold';
    };

    return (
        <div>
        {/* Mobile Sidebar (Headless UI) */}
        <Transition.Root show={sidebarOpen} as={Fragment}>
            <Dialog as="div" className="relative z-50 lg:hidden" onClose={setSidebarOpen}>
            <Transition.Child
                as={Fragment}
                enter="transition-opacity ease-linear duration-300"
                enterFrom="opacity-0"
                enterTo="opacity-100"
                leave="transition-opacity ease-linear duration-300"
                leaveFrom="opacity-100"
                leaveTo="opacity-0"
            >
                <div className="fixed inset-0 bg-gray-900/80" />
            </Transition.Child>

            <div className="fixed inset-0 flex">
                <Transition.Child
                as={Fragment}
                enter="transition ease-in-out duration-300 transform"
                enterFrom="-translate-x-full"
                enterTo="translate-x-0"
                leave="transition ease-in-out duration-300 transform"
                leaveFrom="translate-x-0"
                leaveTo="-translate-x-full"
                >
                <Dialog.Panel className="relative mr-16 flex w-full max-w-xs flex-1">
                    <Transition.Child
                    as={Fragment}
                    enter="ease-in-out duration-300"
                    enterFrom="opacity-0"
                    enterTo="opacity-100"
                    leave="ease-in-out duration-300"
                    leaveFrom="opacity-100"
                    leaveTo="opacity-0"
                    >
                    <div className="absolute left-full top-0 flex w-16 justify-center pt-5">
                        <button type="button" className="-m-2.5 p-2.5" onClick={() => setSidebarOpen(false)}>
                        <span className="sr-only">Close sidebar</span>
                        <XMarkIcon className="h-6 w-6 text-white" aria-hidden="true" />
                        </button>
                    </div>
                    </Transition.Child>
                    {/* Mobile Sidebar Content */}
                    <div className="flex grow flex-col gap-y-5 overflow-y-auto bg-gray-900 px-6 pb-4 ring-1 ring-white/10">
                    <div className="flex h-16 shrink-0 items-center">
                        <span className="text-xl font-bold text-white">ScheduleCentral</span>
                    </div>
                    <nav className="flex flex-1 flex-col">
                        <ul role="list" className="flex flex-1 flex-col gap-y-7">
                        <li>
                            <ul role="list" className="-mx-2 space-y-1">
                            {filteredNav.map((item) => (
                                <li key={item.name}>
                                <NavLink
                                    to={item.href}
                                    onClick={() => setSidebarOpen(false)}
                                    className={({ isActive }) => getNavLinkClass(isActive)}
                                >
                                    <item.icon className="h-6 w-6 shrink-0" aria-hidden="true" />
                                    {item.name}
                                </NavLink>
                                </li>
                            ))}
                            </ul>
                        </li>
                        </ul>
                    </nav>
                    </div>
                </Dialog.Panel>
                </Transition.Child>
            </div>
            </Dialog>
        </Transition.Root>

        {/* Desktop Sidebar */}
        <div className="hidden lg:fixed lg:inset-y-0 lg:z-50 lg:flex lg:w-72 lg:flex-col">
            <div className="flex grow flex-col gap-y-5 overflow-y-auto bg-gray-900 px-6 pb-4">
            <div className="flex h-16 shrink-0 items-center">
                <span className="text-xl font-bold text-white tracking-tight">ScheduleCentral</span>
            </div>
            <nav className="flex flex-1 flex-col">
                <ul role="list" className="flex flex-1 flex-col gap-y-7">
                <li>
                    <ul role="list" className="-mx-2 space-y-1">
                    {filteredNav.map((item) => (
                        <li key={item.name}>
                        <NavLink
                            to={item.href}
                            className={({ isActive }) => getNavLinkClass(isActive)}
                        >
                            <item.icon className="h-6 w-6 shrink-0" aria-hidden="true" />
                            {item.name}
                        </NavLink>
                        </li>
                    ))}
                    </ul>
                </li>
                
                {/* User Profile Section at Bottom */}
                <li className="mt-auto">
                    <a href="#" className="group -mx-2 flex gap-x-3 rounded-md p-2 text-sm font-semibold leading-6 text-gray-400 hover:bg-gray-800 hover:text-white">
                    <div className="h-8 w-8 rounded-full bg-gray-800 flex items-center justify-center border border-gray-600 text-white">
                        <span className="text-xs font-medium">{session.userName.charAt(0).toUpperCase()}</span>
                    </div>
                    <span className="sr-only">Your profile</span>
                    <span aria-hidden="true">{session.userName}</span>
                    </a>
                    
                    <form action="/Identity/Account/Logout?returnUrl=%2F" method="post" className="mt-2">
                        <button type="submit" className="group -mx-2 flex gap-x-3 rounded-md p-2 text-sm font-semibold leading-6 text-gray-400 hover:bg-gray-800 hover:text-red-400 w-full text-left transition-colors duration-150">
                            <ArrowLeftOnRectangleIcon className="h-6 w-6 shrink-0" aria-hidden="true" />
                            Log out
                        </button>
                    </form>
                </li>
                </ul>
            </nav>
            </div>
        </div>

        {/* Main Content Area */}
        <div className="lg:pl-72">
            {/* Sticky Header */}
            <div className="sticky top-0 z-40 flex h-16 shrink-0 items-center gap-x-4 border-b border-gray-200 bg-white px-4 shadow-sm sm:gap-x-6 sm:px-6 lg:px-8">
            <button type="button" className="-m-2.5 p-2.5 text-gray-700 lg:hidden" onClick={() => setSidebarOpen(true)}>
                <span className="sr-only">Open sidebar</span>
                <Bars3Icon className="h-6 w-6" aria-hidden="true" />
            </button>

            {/* Header Content */}
            <div className="flex flex-1 gap-x-4 self-stretch lg:gap-x-6">
                <div className="flex flex-1 items-center">
                <h1 className="text-2xl font-semibold text-gray-900">
                    {/* Dynamically show current page name */}
                    {filteredNav.find(item => item.href === location.pathname)?.name || "Dashboard"}
                </h1>
                </div>
                <div className="flex items-center gap-x-4 lg:gap-x-6">
                {/* Notifications or other header items can go here */}
                <div className="hidden lg:block lg:h-6 lg:w-px lg:bg-gray-900/10" aria-hidden="true" />
                <span className="text-sm font-semibold leading-6 text-gray-900">{session.roles[0] || 'User'}</span>
                </div>
            </div>
            </div>

            {/* Page Content */}
            <main className="py-10">
            <div className="px-4 sm:px-6 lg:px-8">
                {/* This is where your page components will render */}
                <div className="bg-white rounded-lg shadow min-h-[80vh] p-6 border border-gray-200">
                    {children}
                </div>
            </div>
            </main>
        </div>
        </div>
    );
};

export default DashboardLayout;