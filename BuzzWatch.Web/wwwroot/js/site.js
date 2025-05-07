// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Wait for the DOM to be loaded
document.addEventListener('DOMContentLoaded', function() {
    // Mobile menu toggle functionality
    const mobileMenuButton = document.getElementById('mobile-menu-button');
    const mobileMenu = document.getElementById('mobile-menu');
    
    if (mobileMenuButton && mobileMenu) {
        mobileMenuButton.addEventListener('click', function() {
            // Toggle the menu visibility
            const expanded = mobileMenuButton.getAttribute('aria-expanded') === 'true';
            mobileMenuButton.setAttribute('aria-expanded', !expanded);
            mobileMenu.classList.toggle('hidden');
        });
    }
    
    // Dropdown functionality for the admin and user menus
    const adminButton = document.getElementById('adminDropdownBtn');
    const adminMenu = adminButton?.nextElementSibling;
    const userButton = document.getElementById('userDropdownBtn');
    const userMenu = userButton?.nextElementSibling;
    
    // Admin dropdown toggle
    if (adminButton && adminMenu) {
        adminButton.addEventListener('click', function(event) {
            event.stopPropagation(); // Prevent event from bubbling up
            const expanded = adminButton.getAttribute('aria-expanded') === 'true';
            adminButton.setAttribute('aria-expanded', !expanded);
            adminMenu.classList.toggle('hidden');
            
            // Ensure dropdown is positioned correctly
            ensureDropdownVisibility(adminMenu);
            
            // Close user dropdown if open
            if (userMenu && !userMenu.classList.contains('hidden')) {
                userMenu.classList.add('hidden');
                userButton.setAttribute('aria-expanded', 'false');
            }
        });
    }
    
    // User dropdown toggle
    if (userButton && userMenu) {
        userButton.addEventListener('click', function(event) {
            event.stopPropagation(); // Prevent event from bubbling up
            const expanded = userButton.getAttribute('aria-expanded') === 'true';
            userButton.setAttribute('aria-expanded', !expanded);
            userMenu.classList.toggle('hidden');
            
            // Ensure dropdown is positioned correctly
            ensureDropdownVisibility(userMenu);
            
            // Close admin dropdown if open
            if (adminMenu && !adminMenu.classList.contains('hidden')) {
                adminMenu.classList.add('hidden');
                adminButton.setAttribute('aria-expanded', 'false');
            }
        });
    }
    
    // Close dropdowns when clicking outside
    document.addEventListener('click', function(event) {
        if (adminButton && adminMenu && !adminButton.contains(event.target) && !adminMenu.contains(event.target)) {
            adminMenu.classList.add('hidden');
            adminButton.setAttribute('aria-expanded', 'false');
        }
        
        if (userButton && userMenu && !userButton.contains(event.target) && !userMenu.contains(event.target)) {
            userMenu.classList.add('hidden');
            userButton.setAttribute('aria-expanded', 'false');
        }
    });
    
    // Prevent dropdown menu clicks from closing the dropdown
    if (adminMenu) {
        adminMenu.addEventListener('click', function(event) {
            // Only prevent default for dropdown toggles and not for actual links
            if (event.target.classList.contains('dropdown-toggle')) {
                event.stopPropagation();
            }
        });
    }
    
    if (userMenu) {
        userMenu.addEventListener('click', function(event) {
            // Only prevent default for dropdown toggles and not for actual links
            if (event.target.classList.contains('dropdown-toggle')) {
                event.stopPropagation();
            }
        });
    }
    
    // Helper function to ensure dropdown is visible
    function ensureDropdownVisibility(dropdown) {
        if (dropdown.classList.contains('hidden')) return;
        
        // Add a slight delay to ensure the dropdown is fully rendered
        setTimeout(() => {
            const rect = dropdown.getBoundingClientRect();
            const windowHeight = window.innerHeight;
            const windowWidth = window.innerWidth;
            
            // Check if dropdown extends beyond bottom of viewport
            if (rect.bottom > windowHeight) {
                dropdown.style.top = 'auto';
                dropdown.style.bottom = '100%';
                dropdown.style.marginTop = '0';
                dropdown.style.marginBottom = '0.5rem';
            }
            
            // Check if dropdown extends beyond right edge of viewport
            if (rect.right > windowWidth) {
                dropdown.style.right = '0';
                dropdown.style.left = 'auto';
            }
        }, 10);
    }
});
