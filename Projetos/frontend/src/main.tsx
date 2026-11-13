import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { App } from './App';
import { AuthProvider } from './app/Auth';
import { createQueryClient } from './app/queryClient';
import './styles.css';

const queryClient = createQueryClient();
const router = createBrowserRouter([{ path: '*', element: <App /> }]);
createRoot(document.getElementById('root')!).render(<StrictMode><QueryClientProvider client={queryClient}><AuthProvider><RouterProvider router={router} /></AuthProvider></QueryClientProvider></StrictMode>);
