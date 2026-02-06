// import React from 'react';
// import { Routes, Route } from 'react-router-dom';
// import DashboardLayout from './components/DashboardLayout';
// import ScheduleCalendar from './pages/ScheduleCalendar';
// import ManageRooms from './pages/ManageRooms';
// import ManageCourses from './pages/ManageCourses';
// import WelcomeDashboard from './pages/WelcomeDashboard';

// // This is the AppSessionData type from our .cshtml file
// interface AppSession {
//     isAuthenticated: boolean;
//     email: string;
//     firstName: string;
//     roles: string[];
// }

// interface AppProps {
//     session: AppSession;
// }

// const App: React.FC<AppProps> = ({ session }) => {
//     if (!session.isAuthenticated) {
//         // If user is not logged in, show a simple public message.
//         // The .NET _LoginPartial will show the "Login" button.
//         return (
//             <div className="p-8">
//                 <h1 className="text-2xl font-bold">Welcome to ScheduleCentral</h1>
//                 <p className="mt-4 text-lg">Please log in to access your dashboard.</p>
//             </div>
//         );
//     }

//     // If logged in, render the full dashboard layout and routes
//     return (
//         <DashboardLayout session={session}>
//             <Routes>
//                 <Route path="/" element={<WelcomeDashboard session={session} />} />

//                 {/* You can also protect routes here based on role */}

//                 <Route path="/MySchedule" element={<ScheduleCalendar />} />
//                 <Route path="/ViewSchedule" element={<ScheduleCalendar />} />

//                 {/* Routes for ProgramOfficer */}
//                 <Route path="/ManageSchedules" element={<ScheduleCalendar />} />
//                 <Route path="/ManageRooms" element={<ManageRooms />} />
//                 <Route path="/ManageCourses" element={<ManageCourses />} />

//                 {/* Add more routes for Admin, etc. */}
//             </Routes>
//         </DashboardLayout>
//     );
// };

// export default App;
import * as React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import DashboardLayout from './components/DashboardLayout';

// Placeholder components to prevent errors
const WelcomeDashboard = ({ session }: { session: any }) => (
    <div className='p-6 bg-white rounded-xl shadow-lg'>
        <h1 className='text-3xl font-bold text-gray-900'>Welcome back, {session.userName}!</h1>
        <p className='mt-2 text-lg text-indigo-600'>Your role: {session.roles.join(', ') || 'User'}</p>
        <p className='mt-4 text-gray-600'>Use the sidebar to navigate your specific tasks.</p>
    </div>
);
const PlaceholderPage = ({ title }: { title: string }) => (
    <div className='p-6 bg-white rounded-xl shadow-lg'>
        <h1 className='text-3xl font-bold text-gray-900'>{title}</h1>
        <p className='mt-2 text-gray-500'>Content for {title} will be built here.</p>
    </div>
);

interface AppProps {
  session: any;
}

const App: React.FC<AppProps> = ({ session }) => {
  if (!session.isAuthenticated) {
    return (
      <div className="flex min-h-[80vh] flex-1 flex-col justify-center px-6 py-12 lg:px-8">
        <div className="sm:mx-auto sm:w-full sm:max-w-sm">
          <h2 className="mt-10 text-center text-2xl font-bold leading-9 tracking-tight text-gray-900">
            Authentication Required
          </h2>
          <div className="mt-6 text-center">
            <a href="/Identity/Account/Login" className="inline-flex justify-center rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-semibold leading-6 text-white shadow-sm hover:bg-indigo-500" rel="external">
                  Go to Login Page &rarr;
            </a>
          </div>
        </div>
      </div>
    );
  }

  return (
    <DashboardLayout session={session}>
      <Routes>
        <Route path="/" element={<WelcomeDashboard session={session} />} />
        
        <Route path="/tm/reports" element={<PlaceholderPage title="Reports & Analytics" />} />
        <Route path="/dept/management" element={<PlaceholderPage title="Department Management" />} />
        <Route path="/po/courses" element={<PlaceholderPage title="Manage Courses" />} />
        <Route path="/po/rooms" element={<PlaceholderPage title="Manage Rooms" />} />
        <Route path="/po/generate" element={<PlaceholderPage title="Generate Schedule" />} />
        <Route path="/instructor/schedule" element={<PlaceholderPage title="My Teaching Schedule" />} />
        <Route path="/instructor/requests" element={<PlaceholderPage title="Swap Request Management" />} />
        <Route path="/admin/users" element={<PlaceholderPage title="User Management" />} />
        <Route path="/admin/settings" element={<PlaceholderPage title="System Settings" />} />
        
        <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
    </DashboardLayout>
  );
};

export default App;