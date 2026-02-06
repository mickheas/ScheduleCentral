import { ArrowTrendingUpIcon, CalendarDaysIcon, ClockIcon, UserGroupIcon } from '@heroicons/react/24/outline';
import * as React from 'react';

interface AppSession {
userName: string;
roles: string[];
}

const WelcomeDashboard: React.FC<{ session: AppSession }> = ({ session }) => {

// Mock stats data - in a real app, you'd fetch this from an API
const stats = [
    { name: 'Total Classes', stat: '12', icon: CalendarDaysIcon, color: 'bg-blue-500' },
    { name: 'Pending Swap Requests', stat: '4', icon: ClockIcon, color: 'bg-amber-500' },
    { name: 'Active Students', stat: '120', icon: UserGroupIcon, color: 'bg-emerald-500' },
    { name: 'System Health', stat: '98%', icon: ArrowTrendingUpIcon, color: 'bg-indigo-500' },
];

return (
    <div>
    {/* Page Header */}
    <div className="md:flex md:items-center md:justify-between mb-8">
        <div className="min-w-0 flex-1">
        <h2 className="text-2xl font-bold leading-7 text-gray-900 sm:truncate sm:text-3xl sm:tracking-tight">
            Welcome back, {session.userName}
        </h2>
        <p className="mt-1 text-sm text-gray-500">
            Overview of your schedule and administrative tasks.
        </p>
        </div>
        <div className="mt-4 flex md:ml-4 md:mt-0">
        <button type="button" className="inline-flex items-center rounded-md bg-white px-3 py-2 text-sm font-semibold text-gray-900 shadow-sm ring-1 ring-inset ring-gray-300 hover:bg-gray-50">
            View Reports
        </button>
        <button type="button" className="ml-3 inline-flex items-center rounded-md bg-indigo-600 px-3 py-2 text-sm font-semibold text-white shadow-sm hover:bg-indigo-700 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600">
            Create New Schedule
        </button>
        </div>
    </div>

    {/* Stats Grid */}
    <dl className="mt-5 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
        {stats.map((item) => (
        <div key={item.name} className="relative overflow-hidden rounded-lg bg-white px-4 pt-5 pb-12 shadow sm:px-6 sm:pt-6 border border-gray-100">
            <dt>
            <div className={`absolute rounded-md p-3 ${item.color}`}>
                {/* Explicit h-6 w-6 classes ensure icons are not huge */}
                <item.icon className="h-6 w-6 text-white" aria-hidden="true" />
            </div>
            <p className="ml-16 truncate text-sm font-medium text-gray-500">{item.name}</p>
            </dt>
            <dd className="ml-16 flex items-baseline pb-1 sm:pb-7">
            <p className="text-2xl font-semibold text-gray-900">{item.stat}</p>
            </dd>
        </div>
        ))}
    </dl>
    
    {/* Recent Activity / Content Placeholder */}
    <div className="mt-8 grid grid-cols-1 gap-6 lg:grid-cols-2">
        {/* Left Card */}
        <div className="overflow-hidden rounded-lg bg-white shadow border border-gray-100">
            <div className="p-6">
                <h3 className="text-base font-semibold leading-6 text-gray-900">Recent Schedule Changes</h3>
                <div className="mt-6 flow-root">
                    <ul role="list" className="-my-5 divide-y divide-gray-200">
                        <li className="py-4">
                            <div className="flex items-center space-x-4">
                                <div className="min-w-0 flex-1">
                                    <p className="truncate text-sm font-medium text-gray-900">Architectural Design V</p>
                                    <p className="truncate text-sm text-gray-500">Room swapped to Studio B</p>
                                </div>
                                <div>
                                    <span className="inline-flex items-center rounded-full bg-green-50 px-2 py-1 text-xs font-medium text-green-700 ring-1 ring-inset ring-green-600/20">Approved</span>
                                </div>
                            </div>
                        </li>
                        {/* Add more mock items here */}
                    </ul>
                </div>
                <div className="mt-6">
                    <a href="#" className="flex w-full items-center justify-center rounded-md bg-white px-3 py-2 text-sm font-semibold text-gray-900 shadow-sm ring-1 ring-inset ring-gray-300 hover:bg-gray-50 sm:mt-0 sm:w-auto">
                        View all
                    </a>
                </div>
            </div>
        </div>

        {/* Right Card */}
        <div className="overflow-hidden rounded-lg bg-white shadow border border-gray-100">
            <div className="p-6">
                <h3 className="text-base font-semibold leading-6 text-gray-900">Quick Actions</h3>
                <div className="mt-4 grid grid-cols-1 gap-4">
                    <button className="relative block w-full rounded-lg border-2 border-dashed border-gray-300 p-12 text-center hover:border-gray-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2">
                        <CalendarDaysIcon className="mx-auto h-12 w-12 text-gray-400" />
                        <span className="mt-2 block text-sm font-semibold text-gray-900">Initiate Schedule Generation</span>
                    </button>
                </div>
            </div>
        </div>
    </div>
    </div>
);
};

export default WelcomeDashboard;